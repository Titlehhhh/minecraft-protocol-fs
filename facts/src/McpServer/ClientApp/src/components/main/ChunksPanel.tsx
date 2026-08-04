import { useEffect, useMemo, useState } from 'react'
import { fetchChunks, fetchChunkStatus, fetchOwnerChunks, indexChunks, searchChunks } from '../../api/chunks'
import { useUIStore } from '../../store/uiStore'
import type { ChunkKind, ChunkSearchOwner, ChunkStatus, ProtocolRagChunk } from '../../types'

function chips(values: string[], empty = 'none') {
  if (!values.length) return <span className="chunks-muted">{empty}</span>
  return values.slice(0, 8).map(value => <span className="chunks-chip" key={value}>{value}</span>)
}

function compactScore(value: number) {
  return Number.isFinite(value) ? value.toFixed(4) : '0'
}

function defaultOwner() {
  return { kind: 'packet' as const, id: 'play.toClient.resource_pack_send' }
}

export function ChunksPanel() {
  const selectedOwner = useUIStore(state => state.selectedOwner)
  const setSelectedOwner = useUIStore(state => state.setSelectedOwner)
  const setMainView = useUIStore(state => state.setMainView)

  const [status, setStatus] = useState<ChunkStatus | null>(null)
  const [kind, setKind] = useState<'packet' | 'type'>(() => selectedOwner?.kind ?? defaultOwner().kind)
  const [ownerId, setOwnerId] = useState(() => selectedOwner?.id ?? defaultOwner().id)
  const [autoFollow, setAutoFollow] = useState(true)
  const [filterKind, setFilterKind] = useState<ChunkKind>('all')
  const [filter, setFilter] = useState('')
  const [maxChars, setMaxChars] = useState(900)
  const [chunks, setChunks] = useState<ProtocolRagChunk[]>([])
  const [searchQuery, setSearchQuery] = useState('resource pack prompt url hash')
  const [searchLimit, setSearchLimit] = useState(8)
  const [results, setResults] = useState<ChunkSearchOwner[]>([])
  const [loading, setLoading] = useState(false)
  const [indexing, setIndexing] = useState(false)
  const [searching, setSearching] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchChunkStatus()
      .then(setStatus)
      .catch(e => setError(e instanceof Error ? e.message : String(e)))
  }, [])

  useEffect(() => {
    if (!autoFollow || !selectedOwner) return
    setKind(selectedOwner.kind)
    setOwnerId(selectedOwner.id)
  }, [autoFollow, selectedOwner])

  const stats = useMemo(() => {
    const owners = new Set(chunks.map(chunk => `${chunk.ownerKind}:${chunk.ownerId}`))
    const kinds = new Set(chunks.map(chunk => chunk.chunkKind))
    const maxTokens = chunks.reduce((max, chunk) => Math.max(max, chunk.estimatedTokenCount), 0)
    const nearLimit = chunks.filter(chunk => chunk.estimatedTokenCount >= 430).length
    return { owners: owners.size, kinds: kinds.size, maxTokens, nearLimit }
  }, [chunks])

  const currentOwnerLabel = `${kind}:${ownerId || '(empty)'}`

  const loadOwner = async (nextKind = kind, nextOwnerId = ownerId) => {
    const id = nextOwnerId.trim()
    if (!id) return
    setLoading(true)
    setError(null)
    setMessage(null)
    try {
      const result = await fetchOwnerChunks(nextKind, id, maxChars)
      setChunks(result.chunks)
      setSelectedOwner({ kind: nextKind, id })
      setMessage(`Loaded ${result.chunks.length} chunks for ${nextKind}:${id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
      setChunks([])
    } finally {
      setLoading(false)
    }
  }

  const loadFiltered = async () => {
    setLoading(true)
    setError(null)
    setMessage(null)
    try {
      const result = await fetchChunks({ kind: filterKind, filter: filter.trim(), maxChars })
      setChunks(result.chunks)
      setMessage(`Loaded ${result.chunks.length} chunks`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
      setChunks([])
    } finally {
      setLoading(false)
    }
  }

  const runIndex = async () => {
    setIndexing(true)
    setError(null)
    setMessage(null)
    try {
      const result = await indexChunks({ kind: filterKind, filter: filter.trim(), maxChars })
      setMessage(`Indexed ${result.chunks} chunks, ${result.vectors} vectors, dim ${result.vectorSize}`)
      setStatus(await fetchChunkStatus())
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setIndexing(false)
    }
  }

  const runSearch = async () => {
    const query = searchQuery.trim()
    if (!query) return
    setSearching(true)
    setError(null)
    setMessage(null)
    try {
      const result = await searchChunks(query, searchLimit)
      setResults(result.owners)
      setMessage(`Found ${result.owners.length} owners`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
      setResults([])
    } finally {
      setSearching(false)
    }
  }

  const openResultOwner = (owner: ChunkSearchOwner) => {
    if (owner.ownerKind !== 'packet' && owner.ownerKind !== 'type') return
    const nextKind = owner.ownerKind
    setKind(nextKind)
    setOwnerId(owner.ownerId)
    setAutoFollow(false)
    void loadOwner(nextKind, owner.ownerId)
  }

  return (
    <section className="chunks-panel">
      <div className="workbench-hero">
        <div>
          <h2>AI preparation workbench</h2>
          <p>Inspect structural chunks, test semantic retrieval, and prepare deterministic context before any F# generator exists.</p>
        </div>
        <div className="workbench-stage-strip" aria-label="Pipeline stages">
          <span className="stage done">facts</span>
          <span className="stage done">chunks</span>
          <span className={status?.enabled ? 'stage done' : 'stage wait'}>vectors</span>
          <span className="stage wait">job draft</span>
          <span className="stage wait">F# proposal</span>
        </div>
      </div>

      <div className="chunks-toolbar">
        <div className="chunks-title-block">
          <h2>Context target</h2>
          <div className="chunks-subtitle">{currentOwnerLabel}</div>
        </div>
        <label className="inline-check">
          <input type="checkbox" checked={autoFollow} onChange={e => setAutoFollow(e.target.checked)} />
          follow sources
        </label>
        <div className={`chunks-status ${status?.enabled ? 'enabled' : 'disabled'}`}>
          {status?.enabled ? 'vectors ready' : 'vectors disabled'}
        </div>
        <button className="btn-ghost" type="button" onClick={() => fetchChunkStatus().then(setStatus)} disabled={loading || indexing || searching}>
          Refresh
        </button>
      </div>

      <div className="chunks-body">
        <div className="chunks-list">
          <div className="chunks-controls">
            <div className="chunks-card chunks-owner">
              <h2>Selected owner</h2>
              <div className="chunks-row">
                <select value={kind} onChange={e => setKind(e.target.value as 'packet' | 'type')}>
                  <option value="packet">packet</option>
                  <option value="type">type</option>
                </select>
                <input type="text" value={ownerId} onChange={e => setOwnerId(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') void loadOwner() }} />
                <button className="btn-blue" type="button" onClick={() => void loadOwner()} disabled={loading}>Load chunks</button>
              </div>
            </div>

            <div className="chunks-card">
              <h2>Bulk load / index</h2>
              <div className="chunks-row">
                <select value={filterKind} onChange={e => setFilterKind(e.target.value as ChunkKind)}>
                  <option value="all">all</option>
                  <option value="packet">packet</option>
                  <option value="type">type</option>
                </select>
                <input type="text" value={filter} placeholder="filter" onChange={e => setFilter(e.target.value)} />
                <input className="chunks-number" type="number" min={300} max={1200} value={maxChars} onChange={e => setMaxChars(Number(e.target.value) || 900)} />
                <button className="btn-ghost" type="button" onClick={() => void loadFiltered()} disabled={loading}>Preview</button>
                <button className="btn-primary" type="button" onClick={() => void runIndex()} disabled={indexing || !status?.enabled}>Index</button>
              </div>
            </div>
          </div>

          {(message || error) && (
            <div className={error ? 'chunks-error' : 'chunks-message'}>{error ?? message}</div>
          )}

          <div className="chunks-stats">
            <span><b>{chunks.length}</b> chunks</span>
            <span><b>{stats.owners}</b> owners</span>
            <span><b>{stats.kinds}</b> chunk kinds</span>
            <span><b>{stats.maxTokens}</b> max est tokens</span>
            {stats.nearLimit > 0 && <span className="warn"><b>{stats.nearLimit}</b> near 512-token limit</span>}
            {loading && <span><span className="spinner" /> loading</span>}
          </div>

          <div className="chunks-items">
            {chunks.length === 0 ? <div className="chunks-empty">No chunks loaded.</div> : chunks.map(chunk => (
              <article className="chunks-item" key={chunk.id}>
                <div className="chunks-item-head">
                  <span className="chunks-kind">{chunk.chunkKind}</span>
                  <strong>{chunk.ownerId}</strong>
                  <em>{chunk.estimatedTokenCount} tok</em>
                </div>
                <code>{chunk.path} / {chunk.versionRange}</code>
                <pre>{chunk.text}</pre>
                <div className="chunks-meta">
                  <div><span>categories</span>{chips(chunk.categories)}</div>
                  <div><span>hints</span>{chips(chunk.semanticHints)}</div>
                  <div><span>fields</span>{chips(chunk.fields)}</div>
                </div>
              </article>
            ))}
          </div>
        </div>

        <aside className="chunks-side">
          <div className="chunks-card">
            <h2>Vector status</h2>
            <div className="chunks-status-grid">
              <span>embedding</span><b>{status?.embeddingConfigured ? 'configured' : 'missing'}</b>
              <span>qdrant</span><b>{status?.qdrantConfigured ? 'configured' : 'missing'}</b>
              <span>collection</span><code>{status?.collection ?? 'unknown'}</code>
              <span>model</span><code>{status?.embeddingModel || 'unknown'}</code>
              <span>base</span><code>{status?.embeddingBaseUrl || 'unknown'}</code>
            </div>
            {status && !status.enabled && (
              <p className="chunks-muted">Missing: {status.missing.join(', ')}</p>
            )}
          </div>

          <div className="chunks-card">
            <h2>Semantic search</h2>
            <div className="chunks-search">
              <input type="text" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') void runSearch() }} />
              <input className="chunks-number" type="number" min={1} max={50} value={searchLimit} onChange={e => setSearchLimit(Number(e.target.value) || 8)} />
              <button className="btn-blue" type="button" onClick={() => void runSearch()} disabled={searching || !status?.enabled}>Search</button>
            </div>
            {searching && <div className="chunks-muted"><span className="spinner" /> searching</div>}
          </div>

          <div className="chunks-card ai-job-card">
            <h2>Future AI job</h2>
            <div className="job-readiness">
              <span className={ownerId ? 'ready' : ''}>owner</span>
              <span className={chunks.length > 0 ? 'ready' : ''}>context</span>
              <span className={status?.enabled ? 'ready' : ''}>vectors</span>
              <span>runner</span>
            </div>
            <button className="btn-ghost" type="button" onClick={() => setMainView('settings')}>
              Configure models
            </button>
          </div>

          <div className="chunks-results">
            {results.length === 0 ? <div className="chunks-empty">No search results.</div> : results.map(owner => (
              <section className="chunks-result" key={owner.owner}>
                <button className="chunks-result-head" type="button" onClick={() => openResultOwner(owner)}>
                  <span className="chunks-kind">{owner.ownerKind}</span>
                  <strong>{owner.ownerId}</strong>
                  <em>{compactScore(owner.score)}</em>
                </button>
                {owner.chunks.map(chunk => (
                  <div className="chunks-hit" key={chunk.id}>
                    <div><b>{chunk.chunkKind}</b><em>{compactScore(chunk.score)}</em></div>
                    <code>{chunk.path}</code>
                    <p>{chunk.text}</p>
                  </div>
                ))}
              </section>
            ))}
          </div>
        </aside>
      </div>
    </section>
  )
}
