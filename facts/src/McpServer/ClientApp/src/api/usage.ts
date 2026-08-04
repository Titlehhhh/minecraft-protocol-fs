import type { DependencyResult, UsageRecord, UsageSummary, UsageTargetKind } from '../types'

export interface FetchUsageOptions {
  top?: number
  kind?: UsageTargetKind | ''
}

async function readJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: `HTTP ${response.status}` }))
    throw new Error((error as { error?: string }).error ?? `HTTP ${response.status}`)
  }
  return response.json()
}

export function fetchUsage(options: FetchUsageOptions = {}): Promise<UsageSummary[]> {
  const params = new URLSearchParams()
  if (options.top && options.top > 0) params.set('top', String(options.top))
  if (options.kind) params.set('kind', options.kind)
  const query = params.toString()
  return readJson(`/api/usage${query ? `?${query}` : ''}`)
}

export function fetchUsers(id: string): Promise<UsageRecord[]> {
  return readJson(`/api/users/${encodeURIComponent(id)}`)
}

export function fetchDependencies(id: string): Promise<DependencyResult> {
  return readJson(`/api/deps/${encodeURIComponent(id)}`)
}
