import { useEffect } from 'react'
import { Sidebar } from './components/sidebar/Sidebar'
import { Main } from './components/main/Main'
import { ResizeHandle } from './components/shared/ResizeHandle'
import { SaveIndicator } from './components/shared/SaveIndicator'
import { useResize } from './hooks/useResize'
import { useConfigStore } from './store/configStore'
import { usePacketsStore } from './store/packetsStore'
import { useUIStore } from './store/uiStore'
import { fetchProtocolTypesByKind, fetchNativeTypes } from './api/packets'

export function App() {
  const mainView = useUIStore(state => state.mainView)
  const setMainView = useUIStore(state => state.setMainView)

  const { size: sidebarWidth, isDragging, onMouseDown } = useResize({
    direction: 'col',
    min: 200,
    max: 600,
    initial: 310,
  })

  useEffect(() => {
    useConfigStore.getState().load()
    usePacketsStore.getState().loadPackets()
    usePacketsStore.getState().loadStats()
    fetchProtocolTypesByKind()
      .then(typesByKind => {
        useUIStore.getState().setProtocolTypesByKind(typesByKind)
        // Also set flat list for backwards compatibility
        const flatList = Object.values(typesByKind).flat().sort()
        useUIStore.getState().setProtocolTypes(flatList)
      })
      .catch(() => { /* ignore — non-critical */ })
    fetchNativeTypes()
      .then(types => useUIStore.getState().setNativeTypes(types))
      .catch(() => { /* ignore — non-critical */ })
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
      <header>
        <h1>⚡ McProtoNet Generator</h1>
        <span>PacketGenerator MCP Server</span>
        <nav className="header-nav" aria-label="Main view">
          <button
            type="button"
            className={`header-nav-btn ${mainView === 'generator' ? 'active' : ''}`}
            onClick={() => setMainView('generator')}
          >
            ⚙ Генерация
          </button>
          <button
            type="button"
            className={`header-nav-btn ${mainView === 'graph' ? 'active' : ''}`}
            onClick={() => setMainView('graph')}
          >
            🕸 Граф
          </button>
          <button
            type="button"
            className={`header-nav-btn ${mainView === 'usage' ? 'active' : ''}`}
            onClick={() => setMainView('usage')}
          >
            Usage
          </button>
          <button
            type="button"
            className={`header-nav-btn ${mainView === 'chunks' ? 'active' : ''}`}
            onClick={() => setMainView('chunks')}
          >
            Chunks
          </button>
        </nav>
        <div className="header-status">
          <SaveIndicator />
        </div>
      </header>
      <div className="layout">
        <Sidebar style={{ width: sidebarWidth }} />
        <ResizeHandle direction="col" isDragging={isDragging} onMouseDown={onMouseDown} />
        <Main />
      </div>
    </>
  )
}
