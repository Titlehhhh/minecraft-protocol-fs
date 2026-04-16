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
