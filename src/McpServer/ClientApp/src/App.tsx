import { useEffect } from 'react'
import { Sidebar } from './components/sidebar/Sidebar'
import { Main } from './components/main/Main'
import { ResizeHandle } from './components/shared/ResizeHandle'
import { SaveIndicator } from './components/shared/SaveIndicator'
import { useResize } from './hooks/useResize'
import { useConfigStore } from './store/configStore'
import { usePacketsStore } from './store/packetsStore'
import { useUIStore } from './store/uiStore'
import { fetchProtocolTypesByKind, fetchNativeTypes, fetchBuildOrder } from './api/packets'

const navItems = [
  { view: 'protocol', label: 'Protocol' },
  { view: 'workbench', label: 'Workbench' },
  { view: 'graph', label: 'Graph' },
  { view: 'usage', label: 'Usage' },
  { view: 'settings', label: 'Settings' },
] as const

export function App() {
  const mainView = useUIStore(state => state.mainView)
  const setMainView = useUIStore(state => state.setMainView)
  const sourcePanelOpen = useUIStore(state => state.sourcePanelOpen)
  const setSourcePanelOpen = useUIStore(state => state.setSourcePanelOpen)
  const selectedOwner = useUIStore(state => state.selectedOwner)
  const showSources = sourcePanelOpen && mainView !== 'settings'

  const { size: sidebarWidth, isDragging, onMouseDown } = useResize({
    direction: 'col',
    min: 240,
    max: 560,
    initial: 330,
  })

  useEffect(() => {
    useConfigStore.getState().load()
    usePacketsStore.getState().loadPackets()
    usePacketsStore.getState().loadStats()
    fetchProtocolTypesByKind()
      .then(typesByKind => {
        useUIStore.getState().setProtocolTypesByKind(typesByKind)
        const flatList = Object.values(typesByKind).flat().sort()
        useUIStore.getState().setProtocolTypes(flatList)
      })
      .catch(() => { /* non-critical */ })
    fetchNativeTypes()
      .then(types => useUIStore.getState().setNativeTypes(types))
      .catch(() => { /* non-critical */ })
    fetchBuildOrder()
      .then(buildOrder => useUIStore.getState().setBuildOrder(buildOrder))
      .catch(() => { /* non-critical */ })
  }, [])

  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (useConfigStore.getState().saveState === 'dirty') {
        e.preventDefault()
        e.returnValue = ''
      }
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [])

  return (
    <>
      <header className="app-header">
        <div className="brand-block">
          <h1>McProtoNet Workbench</h1>
          <span>PacketGenerator MCP Server</span>
        </div>

        <nav className="header-nav" aria-label="Main view">
          {navItems.map(item => (
            <button
              key={item.view}
              type="button"
              className={`header-nav-btn ${mainView === item.view ? 'active' : ''}`}
              onClick={() => setMainView(item.view)}
            >
              {item.label}
            </button>
          ))}
        </nav>

        <button
          type="button"
          className={`source-toggle ${showSources ? 'active' : ''}`}
          onClick={() => setSourcePanelOpen(!sourcePanelOpen)}
          disabled={mainView === 'settings'}
        >
          Sources
        </button>

        <div className="selected-owner-chip" title={selectedOwner ? selectedOwner.id : undefined}>
          {selectedOwner ? `${selectedOwner.kind}:${selectedOwner.id}` : 'no selection'}
        </div>

        <div className="header-status">
          <SaveIndicator />
        </div>
      </header>

      <div className="layout">
        {showSources && (
          <>
            <Sidebar style={{ width: sidebarWidth }} />
            <ResizeHandle direction="col" isDragging={isDragging} onMouseDown={onMouseDown} />
          </>
        )}
        <Main />
      </div>
    </>
  )
}
