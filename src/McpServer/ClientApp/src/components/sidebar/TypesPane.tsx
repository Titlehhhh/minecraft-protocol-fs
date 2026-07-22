import { useState, useMemo } from 'react'
import type { CSSProperties } from 'react'
import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'
import type { BuildOrderEntry } from '../../api/packets'

const TIER_COLORS: Record<string, string> = {
  tiny: '#3fb950',
  easy: '#58a6ff',
  medium: '#d29922',
  heavy: '#f85149',
}

interface Block {
  key: string
  entries: BuildOrderEntry[]
  recursive: boolean
}

interface Layer {
  layer: number
  blocks: Block[]
}

export function TypesPane() {
  const protocolTypesByKind = useUIStore(s => s.protocolTypesByKind)
  const typesLoaded = useUIStore(s => s.typesLoaded)
  const selectedType = useUIStore(s => s.selectedType)
  const selectType = useUIStore(s => s.selectType)
  const setSelectedOwner = useUIStore(s => s.setSelectedOwner)
  const expandedKinds = useUIStore(s => s.expandedKinds)
  const toggleKindExpanded = useUIStore(s => s.toggleKindExpanded)
  const loadTypeSchema = useGenerationStore(s => s.loadTypeSchema)
  const typeGrouping = useUIStore(s => s.typeGrouping)
  const setTypeGrouping = useUIStore(s => s.setTypeGrouping)
  const buildOrder = useUIStore(s => s.buildOrder)
  const doneTypes = useUIStore(s => s.doneTypes)
  const toggleTypeDone = useUIStore(s => s.toggleTypeDone)
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

  // Structure for the build-order view (independent of doneTypes so it isn't rebuilt on each tick).
  const layers = useMemo<Layer[]>(() => {
    if (!buildOrder) return []
    const q = search.trim().toLowerCase()

    const groupSize = new Map<number, number>()
    for (const e of buildOrder.types) groupSize.set(e.group, (groupSize.get(e.group) ?? 0) + 1)

    const byLayer = new Map<number, BuildOrderEntry[]>()
    for (const e of buildOrder.types) {
      if (q && !e.name.toLowerCase().includes(q)) continue
      if (!byLayer.has(e.layer)) byLayer.set(e.layer, [])
      byLayer.get(e.layer)!.push(e)
    }

    return [...byLayer.keys()].sort((a, b) => a - b).map(layer => {
      const entries = byLayer.get(layer)!
      const blocks: Block[] = []
      const seen = new Map<number, Block>()
      for (const e of entries) {
        const size = groupSize.get(e.group) ?? 1
        if (size > 1) {
          let b = seen.get(e.group)
          if (!b) {
            b = { key: `g${e.group}`, entries: [], recursive: true }
            seen.set(e.group, b)
            blocks.push(b)
          }
          b.entries.push(e)
        } else {
          blocks.push({ key: e.name, entries: [e], recursive: e.recursive })
        }
      }
      return { layer, blocks }
    })
  }, [buildOrder, search])

  const doneCount = useMemo(() => {
    if (!buildOrder) return 0
    return buildOrder.types.reduce((n, e) => n + (doneTypes.has(e.name) ? 1 : 0), 0)
  }, [buildOrder, doneTypes])

  const handleSelectType = (typeId: string) => {
    selectType(typeId)
    setSelectedOwner({ kind: 'type', id: typeId })
    loadTypeSchema(typeId)
  }

  const totalTypes = Object.values(protocolTypesByKind).flat().length

  return (
    <>
      <div className="packets-header">
        <h2>
          Types{' '}
          <span style={{ fontWeight: 400, textTransform: 'none', letterSpacing: 0, color: '#484f58' }}>
            {typesLoaded ? `(${totalTypes})` : ''}
          </span>
        </h2>
        <div style={{ display: 'flex', gap: 4, margin: '6px 0' }}>
          <GroupingButton label="By kind" active={typeGrouping === 'kind'} onClick={() => setTypeGrouping('kind')} />
          <GroupingButton
            label="By build order"
            active={typeGrouping === 'buildOrder'}
            onClick={() => setTypeGrouping('buildOrder')}
          />
        </div>
        <input
          type="text"
          className="packet-search"
          placeholder="Filter..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {typeGrouping === 'buildOrder' && buildOrder && (
        <div
          style={{
            padding: '6px 12px',
            fontSize: 11,
            color: '#8b949e',
            borderBottom: '1px solid #30363d',
            display: 'flex',
            alignItems: 'center',
            gap: 8,
          }}
        >
          <span>{buildOrder.layerCount} layers · simple → complex</span>
          <span style={{ marginLeft: 'auto', color: '#3fb950' }}>
            {doneCount}/{buildOrder.typeCount} done
          </span>
        </div>
      )}

      <div className="packet-list">
        {!typesLoaded ? (
          <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>Loading...</div>
        ) : typeGrouping === 'buildOrder' ? (
          !buildOrder ? (
            <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>Build order unavailable</div>
          ) : layers.length === 0 ? (
            <div style={{ padding: '12px', fontSize: 11, color: '#484f58' }}>No types found</div>
          ) : (
            layers.map(({ layer, blocks }) => (
              <div key={layer}>
                <div
                  className="type-group-header"
                  style={{
                    padding: '8px 12px',
                    background: '#161b22',
                    borderBottom: '1px solid #30363d',
                    userSelect: 'none',
                    fontSize: 12,
                    fontWeight: 500,
                    color: '#79c0ff',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 6,
                  }}
                >
                  Layer {layer}
                  <span style={{ marginLeft: 'auto', fontSize: 11, color: '#484f58' }}>
                    {blocks.reduce((n, b) => n + b.entries.length, 0)}
                  </span>
                </div>
                <div>
                  {blocks.map(block =>
                    block.entries.length > 1 ? (
                      <RecursiveGroup
                        key={block.key}
                        block={block}
                        selectedType={selectedType}
                        doneTypes={doneTypes}
                        onSelect={handleSelectType}
                        onToggleDone={toggleTypeDone}
                      />
                    ) : (
                      <TypeRow
                        key={block.key}
                        entry={block.entries[0]}
                        selected={selectedType === block.entries[0].name}
                        doneTypes={doneTypes}
                        onSelect={handleSelectType}
                        onToggleDone={toggleTypeDone}
                      />
                    )
                  )}
                </div>
              </div>
            ))
          )
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

function GroupingButton({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      style={{
        flex: 1,
        padding: '4px 6px',
        fontSize: 11,
        cursor: 'pointer',
        borderRadius: 4,
        border: `1px solid ${active ? '#388bfd' : '#30363d'}`,
        background: active ? '#1f6feb22' : 'transparent',
        color: active ? '#79c0ff' : '#8b949e',
      }}
    >
      {label}
    </button>
  )
}

/** External deps of a block = deps of its members that are not members themselves. */
function externalDeps(entries: BuildOrderEntry[]): string[] {
  const members = new Set(entries.map(e => e.name))
  const out = new Set<string>()
  for (const e of entries) for (const d of e.deps) if (!members.has(d)) out.add(d)
  return [...out].sort()
}

function TypeRow({
  entry,
  selected,
  doneTypes,
  onSelect,
  onToggleDone,
}: {
  entry: BuildOrderEntry
  selected: boolean
  doneTypes: Set<string>
  onSelect: (id: string) => void
  onToggleDone: (name: string) => void
}) {
  const deps = externalDeps([entry])
  const done = doneTypes.has(entry.name)
  const ready = !done && deps.every(d => doneTypes.has(d))
  const accent = done ? '#238636' : ready ? '#3fb950' : 'transparent'

  return (
    <div
      style={{
        borderBottom: '1px solid #21262d',
        borderLeft: `2px solid ${accent}`,
        background: selected ? '#1f6feb22' : 'transparent',
        opacity: done ? 0.6 : 1,
        padding: '6px 10px 6px 8px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <input
          type="checkbox"
          checked={done}
          onChange={() => onToggleDone(entry.name)}
          title="Mark modelled"
          style={{ cursor: 'pointer', accentColor: '#3fb950' }}
        />
        <span
          onClick={() => onSelect(entry.name)}
          title={entry.name}
          style={{
            cursor: 'pointer',
            fontSize: 12,
            color: '#c9d1d9',
            textDecoration: done ? 'line-through' : 'none',
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          {entry.name}
        </span>
        {entry.recursive && (
          <span title="Self-recursive type" style={{ fontSize: 10, color: '#d29922' }}>
            ↻
          </span>
        )}
        {ready && <span style={badgeStyle('#3fb950')}>ready</span>}
        <span style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontSize: 10, color: TIER_COLORS[entry.tier] ?? '#8b949e' }}>{entry.tier}</span>
          <span style={{ fontSize: 10, color: '#484f58' }}>{entry.score}</span>
        </span>
      </div>
      {deps.length > 0 && <DepChips deps={deps} doneTypes={doneTypes} onSelect={onSelect} />}
    </div>
  )
}

function RecursiveGroup({
  block,
  selectedType,
  doneTypes,
  onSelect,
  onToggleDone,
}: {
  block: Block
  selectedType: string | null
  doneTypes: Set<string>
  onSelect: (id: string) => void
  onToggleDone: (name: string) => void
}) {
  const deps = externalDeps(block.entries)
  const allDone = block.entries.every(e => doneTypes.has(e.name))
  const ready = !allDone && deps.every(d => doneTypes.has(d))

  return (
    <div style={{ borderBottom: '1px solid #21262d', padding: '4px 6px' }}>
      <div
        style={{
          border: '1px solid #d2992255',
          borderRadius: 6,
          background: '#d299220d',
          overflow: 'hidden',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '4px 8px',
            fontSize: 10,
            color: '#d29922',
            borderBottom: '1px solid #d2992233',
          }}
        >
          ↻ recursive group ({block.entries.length})
          {ready && <span style={badgeStyle('#3fb950')}>ready</span>}
        </div>
        {block.entries.map(e => (
          <TypeRow
            key={e.name}
            entry={e}
            selected={selectedType === e.name}
            doneTypes={doneTypes}
            onSelect={onSelect}
            onToggleDone={onToggleDone}
          />
        ))}
        {deps.length > 0 && (
          <div style={{ padding: '4px 8px 6px' }}>
            <span style={{ fontSize: 10, color: '#484f58' }}>needs: </span>
            <DepChips deps={deps} doneTypes={doneTypes} onSelect={onSelect} />
          </div>
        )}
      </div>
    </div>
  )
}

function DepChips({
  deps,
  doneTypes,
  onSelect,
}: {
  deps: string[]
  doneTypes: Set<string>
  onSelect: (id: string) => void
}) {
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 3, marginTop: 4, paddingLeft: 22 }}>
      {deps.map(d => {
        const depDone = doneTypes.has(d)
        return (
          <span
            key={d}
            onClick={() => onSelect(d)}
            title={depDone ? `${d} (done)` : d}
            style={{
              cursor: 'pointer',
              fontSize: 9,
              padding: '1px 5px',
              borderRadius: 3,
              border: `1px solid ${depDone ? '#23863644' : '#30363d'}`,
              background: depDone ? '#23863622' : '#161b22',
              color: depDone ? '#3fb950' : '#8b949e',
            }}
          >
            {depDone ? '✓ ' : ''}
            {d}
          </span>
        )
      })}
    </div>
  )
}

function badgeStyle(color: string): CSSProperties {
  return {
    fontSize: 9,
    fontWeight: 600,
    padding: '0 5px',
    borderRadius: 3,
    color,
    border: `1px solid ${color}55`,
    background: `${color}18`,
    textTransform: 'uppercase',
    letterSpacing: 0.3,
  }
}
