import type { ModelConfig } from '../types'

export async function fetchConfig(): Promise<ModelConfig> {
  const r = await fetch('/api/config')
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json()
}

export async function saveConfig(cfg: ModelConfig): Promise<void> {
  const r = await fetch('/api/config', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(cfg),
  })
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
}
