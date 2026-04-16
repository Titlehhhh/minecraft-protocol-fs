import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'

export function OutputTabs() {
  const mainTab = useUIStore(s => s.mainTab)
  const setMainTab = useUIStore(s => s.setMainTab)
  const protocolTypes = useUIStore(s => s.protocolTypes)
  const codeOutput = useGenerationStore(s => s.codeOutput)
  const promptOutput = useGenerationStore(s => s.promptOutput)

  return (
    <>
      <div className="tab-bar">
        <button
          className={`tab${mainTab === 'code' ? ' active' : ''}`}
          onClick={() => setMainTab('code')}
        >
          ⚙ Code
        </button>
        <button
          className={`tab${mainTab === 'prompt' ? ' active' : ''}`}
          onClick={() => setMainTab('prompt')}
        >
          📋 Prompt
        </button>
        <button
          className={`tab${mainTab === 'types' ? ' active' : ''}`}
          onClick={() => setMainTab('types')}
        >
          🗂 Types
        </button>
      </div>
      <div className={`pane${mainTab === 'code' ? ' active' : ''}`}>
        <pre>{codeOutput}</pre>
      </div>
      <div className={`pane${mainTab === 'prompt' ? ' active' : ''}`}>
        <pre>{promptOutput}</pre>
      </div>
      <div className={`pane${mainTab === 'types' ? ' active' : ''}`}>
        {protocolTypes.length === 0 ? (
          <pre style={{ color: '#484f58' }}>// Loading protocol types...</pre>
        ) : (
          <pre style={{ fontSize: 11, lineHeight: 1.5 }}>
            {`// Protocol types (${protocolTypes.length}) — registered in protodef namespace\n// These can appear as ProtodefCustomType in packet schemas\n\n`}
            {protocolTypes.join('\n')}
          </pre>
        )}
      </div>
    </>
  )
}
