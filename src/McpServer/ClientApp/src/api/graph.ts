import type { ProtocolGraphResponse } from '../types'

export interface FetchGraphOptions {
  ns?: string
  direction?: string
  includeTypes?: boolean
}

export async function fetchProtocolGraph(options: FetchGraphOptions = {}): Promise<ProtocolGraphResponse> {
  const params = new URLSearchParams()
  if (options.ns) params.set('ns', options.ns)
  if (options.direction) params.set('direction', options.direction)
  params.set('includeTypes', String(options.includeTypes ?? true))

  const r = await fetch(`/api/graph?${params.toString()}`)
  if (!r.ok) {
    const e = await r.json().catch(() => ({ error: `HTTP ${r.status}` }))
    throw new Error((e as { error?: string }).error ?? `HTTP ${r.status}`)
  }
  return r.json()
}
