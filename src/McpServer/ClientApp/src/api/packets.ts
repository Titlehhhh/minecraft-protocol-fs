import type { StatsResponse, SchemaData, AssessResponse } from '../types'

export async function fetchPackets(): Promise<Record<string, string[]>> {
  const r = await fetch('/api/packets')
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json()
}

export async function fetchStats(): Promise<StatsResponse> {
  const r = await fetch('/api/stats')
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json()
}

export async function fetchSchema(id: string): Promise<SchemaData> {
  const r = await fetch(`/api/schema/${encodeURIComponent(id)}`)
  if (!r.ok) {
    const e = await r.json().catch(() => ({ error: `HTTP ${r.status}` }))
    throw new Error((e as { error?: string }).error ?? 'Error')
  }
  return r.json()
}

export async function assessPacket(id: string): Promise<AssessResponse> {
  const r = await fetch(`/api/assess/${encodeURIComponent(id)}`)
  const d = await r.json()
  if (!r.ok) throw new Error((d as { error?: string }).error ?? `HTTP ${r.status}`)
  return d as AssessResponse
}
