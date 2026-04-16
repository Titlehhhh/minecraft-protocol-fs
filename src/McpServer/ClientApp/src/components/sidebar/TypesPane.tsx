import { useState, useMemo } from 'react'
import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'

export function TypesPane() {
  const protocolTypes = useUIStore(s => s.protocolTypes)
  const typesLoaded = useUIStore(s => s.typesLoaded)
  const selectedType = useUIStore(s => s.selectedType)
  const selectType = useUIStore(s => s.selectType)
  const loadTypeSchema = useGenerationStore(s => s.loadTypeSchema)
  const [search, setSearch] = useState('')

  const filtered = useMemo(() => {
    if (!search.trim()) return protocolTypes
    const q = search.toLowerCase()
    return protocolTypes.filter(t => t.toLowerCase().includes(q))
  }, [protocolTypes, search])

  const handleSelect = (typeId: string) => {
    selectType(typeId)
    loadTypeSchema(typeId)
  }

  return (
    <>
      <div className="packets-header">
        <h2>
          Types{' '}
          <span style={{ fontWeight: 400, textTransform: 'none', letterSpacing: 0, color: '#484f58' }}>
            {typesLoaded ? `(${protocolTypes.length})` : ''}
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
        ) : filtered.length === 0 ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>No types found</div>
        ) : (
          filtered.map(t => (
            <div
              key={t}
              className={['packet-item', selectedType === t ? 'selected' : ''].filter(Boolean).join(' ')}
              onClick={() => handleSelect(t)}
              title={t}
            >
              {t}
            </div>
          ))
        )}
      </div>
    </>
  )
}
