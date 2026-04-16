import { CSSProperties } from 'react'
import { useUIStore } from '../../store/uiStore'
import { PacketsPane } from './PacketsPane'
import { TypesPane } from './TypesPane'
import { ConfigPane } from './config/ConfigPane'

interface Props {
  style?: CSSProperties
}

export function Sidebar({ style }: Props) {
  const sidebarTab = useUIStore(s => s.sidebarTab)
  const setSidebarTab = useUIStore(s => s.setSidebarTab)

  return (
    <div className="sidebar" style={style}>
      <div className="sidebar-tabs">
        <button
          className={`sidebar-tab${sidebarTab === 'packets' ? ' active' : ''}`}
          onClick={() => setSidebarTab('packets')}
        >
          📦 Packets
        </button>
        <button
          className={`sidebar-tab${sidebarTab === 'types' ? ' active' : ''}`}
          onClick={() => setSidebarTab('types')}
        >
          🗂 Types
        </button>
        <button
          className={`sidebar-tab${sidebarTab === 'config' ? ' active' : ''}`}
          onClick={() => setSidebarTab('config')}
        >
          ⚙️ Config
        </button>
      </div>

      <div className={`sidebar-pane${sidebarTab === 'packets' ? ' active' : ''}`}>
        <PacketsPane />
      </div>
      <div className={`sidebar-pane${sidebarTab === 'types' ? ' active' : ''}`}>
        <TypesPane />
      </div>
      <div className={`sidebar-pane${sidebarTab === 'config' ? ' active' : ''}`}>
        <ConfigPane />
      </div>
    </div>
  )
}
