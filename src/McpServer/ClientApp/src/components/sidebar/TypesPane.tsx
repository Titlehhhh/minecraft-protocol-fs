import { useState, useMemo } from 'react'
import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'

export function TypesPane() {
  const protocolTypesByKind = useUIStore(s => s.protocolTypesByKind)
  const typesLoaded = useUIStore(s => s.typesLoaded)
  const selectedType = useUIStore(s => s.selectedType)
  const selectType = useUIStore(s => s.selectType)
  const expandedKinds = useUIStore(s => s.expandedKinds)
  const toggleKindExpanded = useUIStore(s => s.toggleKindExpanded)
  const loadTypeSchema = useGenerationStore(s => s.loadTypeSchema)
  const [search, setSearch] = useState('')

  const filtered = useMemo(() => {
    if (!search.trim()) return protocolTypesByKind
    
    const q = search.toLowerCase()
    const result: Record<string, string[]> = {}
    
    for (const [kind, types] of Object.entries(protocolTypesByKind)) {
      const filteredTypes = types.filter(t => t.toLowerCase().includes(q))
      if (filteredTypes.length > 0) {
        result[kind] = filteredTypes
      }
    }
    
    return result
  }, [protocolTypesByKind, search])

  const handleSelectType = (typeId: string) => {
    selectType(typeId)
    loadTypeSchema(typeId)
  }

  return (
    <>
      <div className="packets-header">
        <h2>
          Types{' '}
          <span style={{ fontWeight: 400, textTransform: 'none', letterSpacing: 0, color: '#484f58' }}>
            {typesLoaded ? `(${Object.values(protocolTypesByKind).flat().length})` : ''}
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
        {!typesLoaded ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>Loading...</div>
        ) : Object.keys(filtered).length === 0 ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>No types found</div>
        ) : (
          Object.entries(filtered).map(([kind, types]) => (
            <div key={kind}>
              <div
                className="type-group-header"
                onClick={() => toggleKindExpanded(kind)}
                style={{
                  padding: '8px 12px',
                  background: '#161b22',
                  borderBottom: '1px solid #30363d',
                  cursor: 'pointer',
                  userSelect: 'none',
                  fontSize: 12,
                  fontWeight: 500,
                  color: '#79c0ff',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 6,
                }}
              >
                <span style={{ display: 'inline-block', width: 12, textAlign: 'center' }}>
                  {expandedKinds.has(kind) ? '▼' : '▶'}
                </span>
                {kind}
                <span style={{ marginLeft: 'auto', fontSize: 11, color: '#484f58' }}>
                  {types.length}
                </span>
              </div>
              {expandedKinds.has(kind) && (
                <div>
                  {types.map(t => (
                    <div
                      key={t}
                      className={['packet-item', selectedType === t ? 'selected' : ''].filter(Boolean).join(' ')}
                      onClick={() => handleSelectType(t)}
                      title={t}
                    >
                      {t}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </>
  )
}
