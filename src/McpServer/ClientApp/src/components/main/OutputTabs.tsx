import { useUIStore } from '../../store/uiStore'
import { useGenerationStore } from '../../store/generationStore'

export function OutputTabs() {
  const { mainTab, setMainTab } = useUIStore()
  const { codeOutput, promptOutput } = useGenerationStore(s => ({
    codeOutput: s.codeOutput,
    promptOutput: s.promptOutput,
  }))

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
