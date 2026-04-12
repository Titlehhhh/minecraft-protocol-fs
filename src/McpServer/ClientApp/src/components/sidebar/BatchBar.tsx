import { usePacketsStore } from '../../store/packetsStore'
import { useGenerationStore } from '../../store/generationStore'
import { useConfigStore } from '../../store/configStore'

export function BatchBar() {
  const checkedIds = usePacketsStore(s => s.checkedIds)
  const allPackets = usePacketsStore(s => s.allPackets)
  const statsPackets = usePacketsStore(s => s.statsPackets)
  const tierFilter = usePacketsStore(s => s.tierFilter)
  const searchQuery = usePacketsStore(s => s.searchQuery)
  const checkAll = usePacketsStore(s => s.checkAll)
  const clearChecked = usePacketsStore(s => s.clearChecked)
  const runBatch = useGenerationStore(s => s.runBatch)
  const cancelBatch = useGenerationStore(s => s.cancelBatch)
  const isBatchRunning = useGenerationStore(s => s.isBatchRunning)
  const outputBaseDir = useConfigStore(s => s.config.outputBaseDir)

  if (checkedIds.size === 0) return null

  const handleSelectAllVisible = () => {
    const q = searchQuery.toLowerCase().trim()
    const source = statsPackets.length > 0
      ? statsPackets
          .filter(p => (tierFilter === 'all' || p.tier === tierFilter) && (!q || p.id.toLowerCase().includes(q)))
          .slice(0, 400)
          .map(p => p.id)
      : allPackets.filter(p => !q || p.toLowerCase().includes(q)).slice(0, 400)
    checkAll(source)
  }

  const handleRun = () => {
    const ids = [...checkedIds]
    runBatch(ids, outputBaseDir)
  }

  return (
    <div className="batch-bar">
      <span className="batch-bar-count">{checkedIds.size} selected</span>
      <button className="btn-ghost" style={{ fontSize: 11, padding: '4px 8px' }} onClick={handleSelectAllVisible}>
        All
      </button>
      <button className="btn-ghost" style={{ fontSize: 11, padding: '4px 8px' }} onClick={clearChecked}>
        Clear
      </button>
      <button
        className="btn-primary"
        style={{ fontSize: 11, padding: '4px 10px' }}
        disabled={isBatchRunning}
        onClick={handleRun}
      >
        ⚡ Run
      </button>
      {isBatchRunning && (
        <button
          className="btn-ghost"
          style={{ fontSize: 11, padding: '4px 8px', color: '#f85149', borderColor: '#da363655' }}
          onClick={cancelBatch}
        >
          ✕
        </button>
      )}
    </div>
  )
}
