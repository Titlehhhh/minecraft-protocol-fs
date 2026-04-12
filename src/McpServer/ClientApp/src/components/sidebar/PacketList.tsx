import { useMemo } from 'react'
import { usePacketsStore } from '../../store/packetsStore'
import { useGenerationStore } from '../../store/generationStore'
import { PacketItem } from './PacketItem'

export function PacketList() {
  const {
    allPackets, statsPackets, complexityCache,
    tierFilter, searchQuery, selectedId, checkedIds,
    selectPacket, toggleCheck,
  } = usePacketsStore()
  const loadSchema = useGenerationStore(s => s.loadSchema)

  const source = useMemo(() => {
    const q = searchQuery.toLowerCase().trim()
    if (statsPackets.length > 0) {
      return statsPackets
        .filter(p => (tierFilter === 'all' || p.tier === tierFilter) && (!q || p.id.toLowerCase().includes(q)))
        .slice(0, 400)
        .map(p => ({ id: p.id, tier: p.tier, score: p.score }))
    }
    return allPackets
      .filter(p => !q || p.toLowerCase().includes(q))
      .slice(0, 400)
      .map(id => {
        const info = complexityCache[id]
        return { id, tier: info?.tier, score: info?.score }
      })
  }, [statsPackets, allPackets, complexityCache, tierFilter, searchQuery])

  const handleSelect = (id: string) => {
    selectPacket(id)
    loadSchema(id)
  }

  if (!source.length) {
    return (
      <div className="packet-list">
        <div style={{ padding: '10px', color: '#484f58', fontSize: '12px' }}>No results</div>
      </div>
    )
  }

  return (
    <div className="packet-list">
      {source.map(p => (
        <PacketItem
          key={p.id}
          id={p.id}
          tier={p.tier}
          score={p.score}
          isSelected={selectedId === p.id}
          isChecked={checkedIds.has(p.id)}
          onSelect={handleSelect}
          onCheck={toggleCheck}
        />
      ))}
    </div>
  )
}
