import type { GenerateRequest, GenerateResponse, PromptResponse, BatchEvent } from '../types'

export async function generatePacket(
  req: GenerateRequest,
  signal: AbortSignal
): Promise<GenerateResponse> {
  const r = await fetch('/api/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
    signal,
  })
  if (!r.ok) {
    const text = await r.text().catch(() => `HTTP ${r.status}`)
    throw Object.assign(new Error(text), { status: r.status })
  }
  return r.json()
}

export async function buildPrompt(id: string): Promise<PromptResponse> {
  const r = await fetch('/api/prompt', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ id }),
  })
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json()
}

export async function* batchGenerate(
  ids: string[],
  outputBaseDir: string,
  signal: AbortSignal
): AsyncGenerator<BatchEvent> {
  const r = await fetch('/api/generate/batch-ids', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ids, outputBaseDir }),
    signal,
  })
  if (!r.ok) {
    const txt = await r.text().catch(() => `HTTP ${r.status}`)
    throw Object.assign(new Error(txt), { status: r.status })
  }

  const reader = r.body!.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) {
      if (buffer.trim()) yield* parseChunks(buffer.split('\n\n'))
      break
    }
    buffer += decoder.decode(value, { stream: true })
    const chunks = buffer.split('\n\n')
    buffer = chunks.pop()!
    yield* parseChunks(chunks)
  }
}

function* parseChunks(chunks: string[]): Generator<BatchEvent> {
  for (const chunk of chunks) {
    if (!chunk.startsWith('data: ')) continue
    try {
      yield JSON.parse(chunk.slice(6)) as BatchEvent
    } catch {
      // skip malformed
    }
  }
}
