import type {
  ChunkIndexResponse,
  ChunkKind,
  ChunkSearchResponse,
  ChunkStatus,
  ProtocolRagChunkSet,
} from '../types'

async function readJson<T>(response: Response): Promise<T> {
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    const message = data && typeof data === 'object' && 'error' in data
      ? String((data as { error?: unknown }).error)
      : `HTTP ${response.status}`
    throw new Error(message)
  }
  return data as T
}

export interface FetchChunksOptions {
  kind?: ChunkKind
  filter?: string
  maxChars?: number
}

export function fetchChunkStatus(): Promise<ChunkStatus> {
  return fetch('/api/chunks/status').then(readJson<ChunkStatus>)
}

export function fetchChunks(options: FetchChunksOptions = {}): Promise<ProtocolRagChunkSet> {
  const params = new URLSearchParams()
  if (options.kind) params.set('kind', options.kind)
  if (options.filter) params.set('filter', options.filter)
  if (options.maxChars) params.set('maxChars', String(options.maxChars))
  const query = params.toString()
  return fetch(`/api/chunks${query ? `?${query}` : ''}`).then(readJson<ProtocolRagChunkSet>)
}

export function fetchOwnerChunks(kind: 'packet' | 'type', id: string, maxChars?: number): Promise<ProtocolRagChunkSet> {
  const params = new URLSearchParams({ kind })
  if (maxChars) params.set('maxChars', String(maxChars))
  return fetch(`/api/chunks/${encodeURIComponent(id)}?${params.toString()}`).then(readJson<ProtocolRagChunkSet>)
}

export function indexChunks(options: FetchChunksOptions = {}): Promise<ChunkIndexResponse> {
  const params = new URLSearchParams()
  if (options.kind) params.set('kind', options.kind)
  if (options.filter) params.set('filter', options.filter)
  if (options.maxChars) params.set('maxChars', String(options.maxChars))
  const query = params.toString()
  return fetch(`/api/chunks/index${query ? `?${query}` : ''}`, { method: 'POST' }).then(readJson<ChunkIndexResponse>)
}

export function searchChunks(query: string, limit: number): Promise<ChunkSearchResponse> {
  return fetch('/api/chunks/search', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ query, limit }),
  }).then(readJson<ChunkSearchResponse>)
}
