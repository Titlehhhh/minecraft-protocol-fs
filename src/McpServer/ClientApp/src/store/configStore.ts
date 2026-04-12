import { create } from 'zustand'
import { fetchConfig, saveConfig as apiSaveConfig } from '../api/config'
import { usePacketsStore } from './packetsStore'
import type { ModelConfig, SaveState } from '../types'

const DEFAULT_CONFIG: ModelConfig = {
  tiny:   { model: '', reasoningEffort: '', endpoint: '', maxConcurrency: 2 },
  easy:   { model: '', reasoningEffort: '', endpoint: '', maxConcurrency: 4 },
  medium: { model: '', reasoningEffort: '', endpoint: '', maxConcurrency: 4 },
  heavy:  { model: '', reasoningEffort: '', endpoint: '', maxConcurrency: 2 },
  tinyComplexityThreshold: 22,
  easyComplexityThreshold: 20,
  heavyComplexityThreshold: 50,
  temperature: 0,
  maxOutputTokens: 4096,
  inputFormat: 'toon',
  outputBaseDir: '',
  assessor: {
    enabled: false,
    model: '',
    endpoint: '',
    maxOutputTokens: 1024,
    reasoningEffort: '',
  },
}

let saveTimer: ReturnType<typeof setTimeout> | null = null

interface ConfigStore {
  config: ModelConfig
  saveState: SaveState
  saveError: string | null
  load: () => Promise<void>
  update: (patch: Partial<ModelConfig> | ((prev: ModelConfig) => ModelConfig)) => void
  save: () => Promise<void>
}

export const useConfigStore = create<ConfigStore>((set, get) => ({
  config: DEFAULT_CONFIG,
  saveState: 'idle',
  saveError: null,

  async load() {
    try {
      const c = await fetchConfig()
      set({ config: c, saveState: 'idle', saveError: null })
    } catch {
      set({ saveState: 'error', saveError: 'Load failed' })
    }
  },

  update(patch) {
    set(s => ({
      config: typeof patch === 'function' ? patch(s.config) : { ...s.config, ...patch },
      saveState: 'dirty',
    }))
    if (saveTimer) clearTimeout(saveTimer)
    saveTimer = setTimeout(() => get().save(), 800)
  },

  async save() {
    if (saveTimer) { clearTimeout(saveTimer); saveTimer = null }
    set({ saveState: 'saving' })
    try {
      await apiSaveConfig(get().config)
      set({ saveState: 'idle', saveError: null })
      // reload stats after config change (thresholds may have changed)
      usePacketsStore.getState().loadStats()
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Save failed'
      set({ saveState: 'error', saveError: msg })
    }
  },
}))
