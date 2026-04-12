import { Toolbar } from './Toolbar'
import { StatusBar } from './StatusBar'
import { SchemaPanel } from './SchemaPanel'
import { OutputTabs } from './OutputTabs'

export function Main() {
  return (
    <div className="main">
      <Toolbar />
      <StatusBar />
      <SchemaPanel />
      <OutputTabs />
    </div>
  )
}
