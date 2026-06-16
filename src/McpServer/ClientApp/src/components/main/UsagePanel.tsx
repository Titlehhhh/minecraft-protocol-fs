import { useEffect, useMemo, useState } from 'react'
import { fetchDependencies, fetchUsage, fetchUsers } from '../../api/usage'
import type { DependencyResult, UsageRecord, UsageSummary, UsageTargetKind } from '../../types'

const kindOptions: Array<UsageTargetKind | ''> = ['', 'packet', 'type', 'shape', 'native']

function ranges(values: string[], limit = 8) {
  if (values.length <= limit) return values.join(', ')
  return `${values.slice(0, limit).join(', ')} +${values.length - limit}`
}

function targetClass(kind: string) {
  return `usage-kind kind-${kind}`
}

export function UsagePanel() {
  const [kind, setKind] = useState<UsageTargetKind | ''>('type')
  const [top, setTop] = useState(25)
  const [lookupId, setLookupId] = useState('HashedSlot')
  const [usage, setUsage] = useState<UsageSummary[]>([])
  const [users, setUsers] = useState<UsageRecord[]>([])
  const [deps, setDeps] = useState<DependencyResult | null>(null)
  const [selected, setSelected] = useState<UsageSummary | null>(null)
  const [loading, setLoading] = useState(false)
  const [lookupLoading, setLookupLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [lookupError, setLookupError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    fetchUsage({ kind, top })
      .then(result => {
        if (!cancelled) {
          setUsage(result)
          setSelected(current => current && result.some(item => item.targetId === current.targetId) ? current : result[0] ?? null)
        }
      })
      .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : String(e)) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [kind, top])

  const runLookup = async (id = lookupId) => {
    const trimmed = id.trim()
    if (!trimmed) return
    setLookupLoading(true)
    setLookupError(null)
    try {
      const [nextUsers, nextDeps] = await Promise.all([
        fetchUsers(trimmed),
        fetchDependencies(trimmed).catch(() => null),
      ])
      setUsers(nextUsers)
      setDeps(nextDeps)
    } catch (e) {
      setLookupError(e instanceof Error ? e.message : String(e))
      setUsers([])
      setDeps(null)
    } finally {
      setLookupLoading(false)
    }
  }

  const filteredUsers = useMemo(() => users.slice(0, 80), [users])
  const dependencies = deps?.dependencies.slice(0, 80) ?? []

  return (
    <section className="usage-panel">
      <div className="usage-toolbar">
        <div className="usage-title-block">
          <h2>Usage</h2>
          <div className="usage-subtitle">Compact packet/type/native/shape statistics</div>
        </div>
        <select value={kind} onChange={e => setKind(e.target.value as UsageTargetKind | '')}>
          {kindOptions.map(option => <option key={option || 'all'} value={option}>{option || 'all'}</option>)}
        </select>
        <input type="number" min={1} max={250} value={top} onChange={e => setTop(Number(e.target.value) || 25)} />
        {loading && <span className="usage-loading"><span className="spinner" /> loading</span>}
      </div>

      <div className="usage-body">
        <div className="usage-list">
          {error && <div className="usage-error">{error}</div>}
          <table>
            <thead>
              <tr><th>target</th><th>uses</th><th>owners</th><th>ranges</th></tr>
            </thead>
            <tbody>
              {usage.map(item => (
                <tr key={item.targetId} className={selected?.targetId === item.targetId ? 'active' : ''} onClick={() => { setSelected(item); setLookupId(item.targetId); void runLookup(item.targetId) }}>
                  <td>
                    <span className={targetClass(item.targetKind)}>{item.targetKind}</span>
                    <strong>{item.label}</strong>
                    <code>{item.path}</code>
                  </td>
                  <td>{item.usageCount}</td>
                  <td>{item.ownerCount}</td>
                  <td>{ranges(item.versionRanges, 5)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <aside className="usage-side">
          <div className="usage-card">
            <h2>Lookup</h2>
            <div className="usage-lookup">
              <input type="text" value={lookupId} onChange={e => setLookupId(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') void runLookup() }} />
              <button className="btn-blue" type="button" onClick={() => void runLookup()} disabled={lookupLoading}>Run</button>
            </div>
            {lookupLoading && <div className="usage-muted">Loading...</div>}
            {lookupError && <div className="usage-error">{lookupError}</div>}
          </div>

          <div className="usage-card">
            <h2>Users</h2>
            {filteredUsers.length === 0 ? <div className="usage-muted">No users loaded.</div> : (
              <div className="usage-records">
                {filteredUsers.map((record, index) => (
                  <div className="usage-record" key={`${record.ownerId}:${record.versionRange}:${record.fieldPath ?? index}`}>
                    <div><span className={targetClass(record.ownerKind)}>{record.ownerKind}</span><strong>{record.ownerId}</strong></div>
                    <code>{record.ownerPath}</code>
                    <p>{record.versionRange}{record.fieldPath ? ` / ${record.fieldPath}` : ''}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="usage-card">
            <h2>Dependencies</h2>
            {deps && <div className="usage-muted"><code>{deps.path}</code></div>}
            {dependencies.length === 0 ? <div className="usage-muted">No dependencies loaded.</div> : (
              <div className="usage-records">
                {dependencies.map(dep => (
                  <div className="usage-record" key={dep.targetId}>
                    <div><span className={targetClass(dep.targetKind)}>{dep.targetKind}</span><strong>{dep.label}</strong><em>{dep.usageCount}</em></div>
                    <code>{dep.path}</code>
                    <p>{ranges(dep.fieldPaths, 4)}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </aside>
      </div>
    </section>
  )
}
