import { useState } from 'react'
import { useUIStore } from '../../store/uiStore'

export function NativeTypesPane() {
  const nativeTypes = useUIStore(s => s.nativeTypes)
  const nativeTypesLoaded = useUIStore(s => s.nativeTypesLoaded)
  const [search, setSearch] = useState('')

  const filtered = search.trim()
    ? nativeTypes.filter(t => t.toLowerCase().includes(search.toLowerCase()))
    : nativeTypes

  return (
    <>
      <div className="packets-header">
        <h2>
          Native{' '}
          <span style={{ fontWeight: 400, textTransform: 'none', letterSpacing: 0, color: '#484f58' }}>
            {nativeTypesLoaded ? `(${nativeTypes.length})` : ''}
          </span>
        </h2>
        <input
          type="text"
          className="packet-search"
          placeholder="Filter..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>
      <div className="packet-list">
        {!nativeTypesLoaded ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>Loading...</div>
        ) : filtered.length === 0 ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>No native types found</div>
        ) : (
          filtered.map(t => (
            <div
              key={t}
              className="packet-item"
              title={t}
              style={{ fontFamily: 'monospace', fontSize: 12 }}
            >
              {t}
            </div>
          ))
        )}
      </div>
    </>
  )
}
