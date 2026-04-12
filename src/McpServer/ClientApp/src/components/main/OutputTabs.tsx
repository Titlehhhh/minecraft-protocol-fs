import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'

export function OutputTabs() {
  const mainTab = useUIStore(s => s.mainTab)
  const setMainTab = useUIStore(s => s.setMainTab)
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
      </div>
      <div className={`pane${mainTab === 'code' ? ' active' : ''}`}>
        <pre>{codeOutput}</pre>
      </div>
      <div className={`pane${mainTab === 'prompt' ? ' active' : ''}`}>
        <pre>{promptOutput}</pre>
      </div>
    </>
  )
}
