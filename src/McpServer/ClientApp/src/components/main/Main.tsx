import { useUIStore } from '../../store/uiStore'
import { Toolbar } from './Toolbar'
import { StatusBar } from './StatusBar'
import { SchemaPanel } from './SchemaPanel'
import { OutputTabs } from './OutputTabs'
import { GraphPanel } from './GraphPanel'

export function Main() {
  const mainView = useUIStore(state => state.mainView)

  if (mainView === 'graph') {
    return (
      <div className="main">
        <GraphPanel />
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
