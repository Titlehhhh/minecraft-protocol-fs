import { useUIStore } from '../../store/uiStore'
import { Toolbar } from './Toolbar'
import { StatusBar } from './StatusBar'
import { SchemaPanel } from './SchemaPanel'
import { OutputTabs } from './OutputTabs'
import { GraphPanel } from './GraphPanel'
import { UsagePanel } from './UsagePanel'
import { ChunksPanel } from './ChunksPanel'

export function Main() {
  const mainView = useUIStore(state => state.mainView)

  if (mainView === 'graph') {
    return (
      <div className="main">
        <GraphPanel />
      </div>
    )
  }

  if (mainView === 'usage') {
    return (
      <div className="main">
        <UsagePanel />
      </div>
    )
  }

  if (mainView === 'chunks') {
    return (
      <div className="main">
        <ChunksPanel />
      </div>
    )
  }

  return (
    <div className="main">
      <Toolbar />
      <StatusBar />
      <SchemaPanel />
      <OutputTabs />
    </div>
  )
}
