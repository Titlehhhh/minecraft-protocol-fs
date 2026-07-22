import { CSSProperties } from 'react'
import { useUIStore } from '../../store/uiStore'
import { PacketsPane } from './PacketsPane'
import { TypesPane } from './TypesPane'
import { NativeTypesPane } from './NativeTypesPane'

interface Props {
  style?: CSSProperties
}

const sourceTabs = [
  { id: 'packets', label: 'Packets' },
  { id: 'types', label: 'Types' },
  { id: 'native', label: 'Native' },
] as const

export function Sidebar({ style }: Props) {
  const sidebarTab = useUIStore(s => s.sidebarTab)
  const setSidebarTab = useUIStore(s => s.setSidebarTab)

  return (
    <aside className="sidebar" style={style}>
      <div className="sidebar-title">
        <strong>Protocol sources</strong>
        <span>facts from protocol access layer</span>
      </div>

      <div className="sidebar-tabs">
        {sourceTabs.map(tab => (
          <button
            key={tab.id}
            className={`sidebar-tab${sidebarTab === tab.id ? ' active' : ''}`}
            onClick={() => setSidebarTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className={`sidebar-pane${sidebarTab === 'packets' ? ' active' : ''}`}>
        <PacketsPane />
      </div>
      <div className={`sidebar-pane${sidebarTab === 'types' ? ' active' : ''}`}>
        <TypesPane />
      </div>
      <div className={`sidebar-pane${sidebarTab === 'native' ? ' active' : ''}`}>
        <NativeTypesPane />
      </div>
    </aside>
  )
}
