import { useEffect } from 'react'
import { Sidebar } from './components/sidebar/Sidebar'
import { Main } from './components/main/Main'
import { ResizeHandle } from './components/shared/ResizeHandle'
import { SaveIndicator } from './components/shared/SaveIndicator'
import { useResize } from './hooks/useResize'
import { useConfigStore } from './store/configStore'
import { usePacketsStore } from './store/packetsStore'

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
