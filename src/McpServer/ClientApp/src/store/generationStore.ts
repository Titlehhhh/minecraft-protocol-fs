import { create } from 'zustand'
import { generatePacket, buildPrompt as apiBuildPrompt, batchGenerate } from '../api/generate'
import { assessPacket, fetchSchema } from '../api/packets'
import { useConfigStore } from './configStore'
import { usePacketsStore } from './packetsStore'
import { useUIStore } from './uiStore'
import { escHtml, si, buildUsageHtml } from '../utils/html'
import type { SchemaData } from '../types'

interface SchemaState {
  visible: boolean
  loadedFor: string | null
  data: SchemaData | null
  loading: boolean
  error: string | null
}

interface GenerationStore {
  statusHtml: string
  codeOutput: string
  promptOutput: string
  isGenerating: boolean
  isAssessing: boolean
  abortController: AbortController | null
  isBatchRunning: boolean
  batchAbortController: AbortController | null
  schema: SchemaState

  generate: (id: string, andSave: boolean) => Promise<void>
  buildPrompt: (id: string) => Promise<void>
  assess: (id: string) => Promise<void>
  cancel: () => void
  clearOutput: () => void
  loadSchema: (id: string) => Promise<void>
  toggleSchema: (id: string) => void
  runBatch: (ids: string[], outputBaseDir: string) => Promise<void>
  cancelBatch: () => void
}

export const useGenerationStore = create<GenerationStore>((set, get) => ({
  statusHtml: '<span>Ready</span>',
  codeOutput: '// Select a packet from the list or type an ID, then click Generate',
  promptOutput: '// Click "📋 Prompt" to build and inspect the prompt',
  isGenerating: false,
  isAssessing: false,
  abortController: null,
  isBatchRunning: false,
  batchAbortController: null,
  schema: { visible: false, loadedFor: null, data: null, loading: false, error: null },

  async generate(id, andSave) {
    if (!id) return
    const cfgStore = useConfigStore.getState()
    if (cfgStore.saveState === 'dirty') await cfgStore.save()

    const outputBaseDir = andSave ? (cfgStore.config.outputBaseDir.trim() || null) : null
    if (andSave && !outputBaseDir) {
      set({ statusHtml: '<span class="badge err">No output directory set</span> &nbsp; Configure it in Settings' })
      return
    }

    const ac = new AbortController()
    set({ isGenerating: true, abortController: ac })
    useUIStore.getState().setMainTab('code')
    const saveLabel = andSave ? ' &amp; Save' : ''
    set({
      codeOutput: '// Generating...',
      statusHtml: `<span class="spinner"></span>&nbsp; Generating${saveLabel} <b>${escHtml(id)}</b>...`,
    })

    const t0 = performance.now()
    try {
      const data = await generatePacket({ id, outputBaseDir }, ac.signal)
      const clientMs = Math.round(performance.now() - t0)

      if (data.systemPrompt) {
        set({
          promptOutput:
            `// ⚠️  Heavy packet — returned for Claude to handle\n` +
            `// Complexity: ${data.complexityScore ?? '?'}  Tokens: ${data.tokenCount}\n\n` +
            `// ══ SYSTEM PROMPT ══\n${data.systemPrompt}\n\n` +
            `// ══ USER PROMPT ══\n${data.userPrompt}`,
          codeOutput: '// Heavy packet — see Prompt tab',
          statusHtml: `<span class="badge warn">Heavy → Claude</span> &nbsp;${si('Tokens', data.tokenCount ?? '?')} &nbsp;${si('Client', clientMs + 'ms')}`,
        })
        useUIStore.getState().setMainTab('prompt')
        return
      }

      set({
        codeOutput: data.code ?? '// (empty response)',
        statusHtml:
          `<span class="badge ok">✓ OK</span>` +
          ` &nbsp;${si('Model', data.model ?? '?')}` +
          (data.reasoningTokens ? ` &nbsp;${si('Reasoning', data.reasoningTokens + ' tok')}` : '') +
          buildUsageHtml(data) +
          ` &nbsp;${si('Server', (data.elapsedMs ?? '?') + 'ms')}` +
          ` &nbsp;${si('Client', clientMs + 'ms')}` +
          (data.savedTo ? ` &nbsp;<span style="color:#3fb950;font-size:11px" title="${escHtml(data.savedTo)}">💾 Saved</span>` : ''),
      })
    } catch (e) {
      const err = e as Error & { status?: number }
      if (err.name === 'AbortError') {
        set({ codeOutput: '// Cancelled', statusHtml: '<span class="badge warn">Cancelled</span>' })
      } else {
        set({
          codeOutput: `// Error:\n// ${err.message}`,
          statusHtml: `<span class="badge err">Error${err.status ? ' ' + err.status : ''}</span>&nbsp; ${escHtml(err.message.slice(0, 120))}`,
        })
      }
    } finally {
      set({ isGenerating: false, abortController: null })
    }
  },

  async buildPrompt(id) {
    if (!id) return
    set({ isGenerating: true })
    useUIStore.getState().setMainTab('prompt')
    set({
      promptOutput: '// Building prompt...',
      statusHtml: `<span class="spinner"></span>&nbsp; Building prompt for <b>${escHtml(id)}</b>...`,
    })
    try {
      const data = await apiBuildPrompt(id)
      set({
        promptOutput:
          `// Tokens — system: ${data.systemTokens}  user: ${data.userTokens}  total: ${data.tokenCount}\n\n` +
          `// ══ SYSTEM PROMPT ══\n${data.system}\n\n` +
          `// ══ USER PROMPT ══\n${data.user}`,
        statusHtml:
          `<span class="badge ok">Prompt ready</span>` +
          ` &nbsp;${si('Sys', data.systemTokens + ' tok')}` +
          ` &nbsp;${si('Packet', data.userTokens + ' tok')}` +
          ` &nbsp;${si('Total', data.tokenCount + ' tok')}`,
      })
    } catch (e) {
      set({ statusHtml: `<span class="badge err">Error</span>&nbsp; ${escHtml((e as Error).message)}` })
    } finally {
      set({ isGenerating: false })
    }
  },

  async assess(id) {
    if (!id) return
    set({
      isAssessing: true,
      statusHtml: `<span class="spinner"></span>&nbsp; Assessing <b>${escHtml(id)}</b>...`,
    })
    try {
      const d = await assessPacket(id)
      const scoreHtml = d.llmScore != null ? ` · llm score: <b style="color:#c9d1d9">${d.llmScore}</b>` : ''
      const reasonHtml = d.reason
        ? ` · <span style="color:#8b949e;font-style:italic">${escHtml(d.reason)}</span>`
        : ''
      set({
        statusHtml:
          si('id', escHtml(id)) +
          si('struct', d.structuralScore) +
          `<span><span style="color:#484f58">tier:</span> <span class="ctier ${d.tier}">${d.tier.toUpperCase()}</span></span>` +
          scoreHtml +
          reasonHtml,
      })
      usePacketsStore.getState().cacheComplexity(id, { score: d.structuralScore, tier: d.tier })
    } catch (e) {
      set({ statusHtml: `<span style="color:#f85149">Assess failed: ${escHtml((e as Error).message)}</span>` })
    } finally {
      set({ isAssessing: false })
    }
  },

  cancel() {
    get().abortController?.abort()
    set({ abortController: null })
  },

  clearOutput() {
    set({
      codeOutput: '// Cleared',
      promptOutput: '// Cleared',
      statusHtml: '<span>Ready</span>',
    })
  },

  async loadSchema(id) {
    const current = get().schema
    if (current.loadedFor === id && current.visible) return
    set(s => ({ schema: { ...s.schema, visible: true, loadedFor: id, loading: true, error: null, data: null } }))
    try {
      const data = await fetchSchema(id)
      usePacketsStore.getState().cacheComplexity(id, { score: data.complexityScore, tier: data.tier })
      set(s => ({ schema: { ...s.schema, loading: false, data } }))
    } catch (e) {
      set(s => ({ schema: { ...s.schema, loading: false, error: (e as Error).message } }))
    }
  },

  toggleSchema(id) {
    const { schema } = get()
    if (schema.visible && schema.loadedFor === id) {
      set(s => ({ schema: { ...s.schema, visible: false } }))
    } else {
      if (id) get().loadSchema(id)
      else set(s => ({ schema: { ...s.schema, visible: !s.schema.visible } }))
    }
  },

  async runBatch(ids, outputBaseDir) {
    const cfgStore = useConfigStore.getState()
    if (cfgStore.saveState === 'dirty') await cfgStore.save()

    const ac = new AbortController()
    set({ isBatchRunning: true, batchAbortController: ac })
    useUIStore.getState().setMainTab('code')

    const lines: string[] = [`// Batch generate — ${ids.length} packets → ${outputBaseDir}\n`]
    let done = 0, ok = 0, err = 0
    let finished = false

    const redraw = () => set({ codeOutput: lines.join('\n') })

    set({ statusHtml: `<span class="spinner"></span>&nbsp; Batch: 0 / ${ids.length}` })
    redraw()

    try {
      for await (const event of batchGenerate(ids, outputBaseDir, ac.signal)) {
        if (event.type === 'packet') {
          done++
          if (event.success) {
            ok++
            lines.push(`✓  ${event.id}${event.savedTo ? '  → saved' : ''}  [${event.model ?? '?'}, ${event.elapsedMs}ms]`)
          } else {
            err++
            lines.push(`✗  ${event.id}  ERROR: ${event.error ?? '?'}`)
          }
          set({ statusHtml: `<span class="spinner"></span>&nbsp; Batch: ${done} / ${ids.length}  ok=${ok} err=${err}` })
          redraw()
        } else if (event.type === 'done') {
          finished = true
          lines.push(`\n// Done — ${event.ok} ok, ${event.err} errors`)
          set({
            statusHtml:
              `<span class="badge ok">Batch done</span>` +
              ` &nbsp;${si('Total', event.total)} &nbsp;${si('OK', event.ok)}` +
              (event.err > 0 ? ` &nbsp;<span class="badge err">${event.err} errors</span>` : ''),
            isBatchRunning: false,
            batchAbortController: null,
          })
          redraw()
          break
        }
      }
    } catch (e) {
      const err2 = e as Error
      if (err2.name === 'AbortError') {
        lines.push('\n// Cancelled')
        set({ statusHtml: '<span class="badge warn">Cancelled</span>' })
      } else {
        lines.push(`\n// Network error: ${err2.message}`)
        set({ statusHtml: `<span class="badge err">Network Error</span>&nbsp; ${escHtml(err2.message)}` })
      }
      redraw()
    } finally {
      if (!finished) {
        set({ isBatchRunning: false, batchAbortController: null })
      }
    }
  },

  cancelBatch() {
    get().batchAbortController?.abort()
    set({ batchAbortController: null })
  },
}))
