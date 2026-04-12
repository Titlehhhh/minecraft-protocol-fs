import { useConfigStore } from '../../../store/configStore'
import { EffortPicker } from './EffortPicker'
import type { ReasoningEffort } from '../../../types'

export function AssessorSection() {
  const { config, update } = useConfigStore()
  const { assessor } = config

  const set = (patch: Partial<typeof assessor>) =>
    update(prev => ({ ...prev, assessor: { ...prev.assessor, ...patch } }))

  return (
    <div className="sidebar-section">
      <h2>Complexity Assessor</h2>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
        <label style={{ margin: 0, flex: 1 }}>LLM assessor</label>
        <button
          className={`effort-btn${assessor.enabled ? ' active' : ''}`}
          style={{ width: 56 }}
          onClick={() => set({ enabled: !assessor.enabled })}
        >
          {assessor.enabled ? 'ON' : 'OFF'}
        </button>
      </div>
      {assessor.enabled && (
        <>
          <label>Model</label>
          <input
            type="text"
            value={assessor.model}
            placeholder="e.g. qwen/qwen3-30b-a3b"
            style={{ marginBottom: 6 }}
            onChange={e => set({ model: e.target.value })}
          />
          <label>
            Endpoint <span style={{ color: '#484f58' }}>(empty = OpenRouter)</span>
          </label>
          <input
            type="text"
            value={assessor.endpoint}
            placeholder="http://localhost:1234/v1 for LM Studio"
            style={{ marginBottom: 6 }}
            onChange={e => set({ endpoint: e.target.value })}
          />
          <label>Max output tokens</label>
          <input
            type="number"
            value={assessor.maxOutputTokens}
            min={64}
            max={16384}
            step={64}
            style={{ marginBottom: 6 }}
            onChange={e => set({ maxOutputTokens: Number(e.target.value) })}
          />
          <label>
            Reasoning <span style={{ color: '#484f58' }}>(off = faster + no token waste)</span>
          </label>
          <EffortPicker
            value={assessor.reasoningEffort as ReasoningEffort}
            onChange={v => set({ reasoningEffort: v })}
            includeXHigh={false}
          />
        </>
      )}
    </div>
  )
}
