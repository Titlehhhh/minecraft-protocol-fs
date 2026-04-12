import { usePacketsStore } from '../../store/packetsStore'
import { TierCards } from './TierCards'
import { PacketList } from './PacketList'
import { BatchBar } from './BatchBar'

export function PacketsPane() {
  const { allPackets, totalCount, searchQuery, setSearchQuery } = usePacketsStore(s => ({
    allPackets: s.allPackets,
    totalCount: s.totalCount,
    searchQuery: s.searchQuery,
    setSearchQuery: s.setSearchQuery,
  }))

  const count = totalCount || allPackets.length

  return (
    <>
      <div className="packets-header">
        <h2>
          Packets{' '}
          <span style={{ fontWeight: 400, textTransform: 'none', letterSpacing: 0, color: '#484f58' }}>
            {count > 0 ? `(${count})` : ''}
          </span>
        </h2>
        <TierCards />
        <input
          type="text"
          className="packet-search"
          placeholder="Filter..."
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
        />
      </div>
      <PacketList />
      <BatchBar />
    </>
  )
}
