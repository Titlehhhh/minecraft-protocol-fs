import type { OpenRouterModelSearchResponse } from '../types'

export type OpenRouterModelKind = 'chat' | 'embedding'

export async function searchOpenRouterModels(
  params: { query: string; kind: OpenRouterModelKind; sort?: string },
  signal?: AbortSignal,
): Promise<OpenRouterModelSearchResponse> {
  const qs = new URLSearchParams({
    kind: params.kind,
    sort: params.sort ?? 'most-popular',
  })
  const query = params.query.trim()
  if (query) qs.set('q', query)

  const r = await fetch(`/api/models/openrouter?${qs.toString()}`, { signal })
  if (!r.ok) throw new Error(`HTTP ${r.status}`)
  return r.json()
}
