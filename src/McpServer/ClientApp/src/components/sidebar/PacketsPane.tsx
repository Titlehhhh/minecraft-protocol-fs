import { usePacketsStore } from '../../store/packetsStore'
import { TierCards } from './TierCards'
import { PacketList } from './PacketList'
import { BatchBar } from './BatchBar'

export function PacketsPane() {
  const allPackets = usePacketsStore(s => s.allPackets)
  const totalCount = usePacketsStore(s => s.totalCount)
  const searchQuery = usePacketsStore(s => s.searchQuery)
  const setSearchQuery = usePacketsStore(s => s.setSearchQuery)

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
