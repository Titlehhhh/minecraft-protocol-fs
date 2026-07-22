import { useUIStore } from '../../store/uiStore'
import { Toolbar } from './Toolbar'
import { StatusBar } from './StatusBar'
import { SchemaPanel } from './SchemaPanel'
import { OutputTabs } from './OutputTabs'
import { GraphPanel } from './GraphPanel'
import { UsagePanel } from './UsagePanel'
import { ChunksPanel } from './ChunksPanel'
import { ConfigPane } from '../sidebar/config/ConfigPane'

function ProtocolPanel() {
  return (
    <>
      <Toolbar />
      <StatusBar />
      <SchemaPanel />
      <OutputTabs />
    </>
  )
}

function SettingsPanel() {
  return (
    <section className="settings-panel">
      <div className="settings-header">
        <div>
          <h2>Settings</h2>
          <p>Model tiers, deterministic generation options, output path, and assessor settings.</p>
        </div>
      </div>
      <ConfigPane />
    </section>
  )
}

export function Main() {
  const mainView = useUIStore(state => state.mainView)

  return (
    <main className="main">
      {mainView === 'protocol' && <ProtocolPanel />}
      {mainView === 'workbench' && <ChunksPanel />}
      {mainView === 'graph' && <GraphPanel />}
      {mainView === 'usage' && <UsagePanel />}
      {mainView === 'settings' && <SettingsPanel />}
    </main>
  )
}
