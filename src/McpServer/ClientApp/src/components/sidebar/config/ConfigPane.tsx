import { useState } from 'react'
import { useConfigStore } from '../../../store/configStore'
import { TierRow, TIER_ROW_DEFS } from './TierRow'
import { AssessorSection } from './AssessorSection'
import { PresetsSection } from './PresetsSection'
import type { Tier, InputFormat } from '../../../types'

export function ConfigPane() {
  const [focusedTier, setFocusedTier] = useState<Tier>('easy')
  const config = useConfigStore(s => s.config)
  const update = useConfigStore(s => s.update)

  return (
    <div className="config-pane-inner">
      <div className="sidebar-section">
        <h2>Model Tiers</h2>

        {TIER_ROW_DEFS.map(def => (
          <TierRow key={def.tier} def={def} onFocusModel={setFocusedTier} />
        ))}

        <div className="row2" style={{ marginTop: 10 }}>
          <div>
            <label title="0 = deterministic greedy decoding. Higher values → more creative but less consistent output.">
              Temperature <span style={{ color: '#484f58' }}>ⓘ</span>
            </label>
            <input
              type="number"
              value={config.temperature}
              min={0}
              max={2}
              step={0.05}
              onChange={e => update({ temperature: parseFloat(e.target.value) || 0 })}
            />
          </div>
          <div>
            <label title="Max tokens the model can output. Increase for large packets. Thinking models need 8192+.">
              Max output tokens <span style={{ color: '#484f58' }}>ⓘ</span>
            </label>
            <input
              type="number"
              value={config.maxOutputTokens}
              min={256}
              max={32768}
              step={256}
              onChange={e => update({ maxOutputTokens: parseInt(e.target.value) || 4096 })}
            />
          </div>
        </div>

        <div className="row2" style={{ marginTop: 8 }}>
          <div>
            <label title="Nucleus sampling (0–1). When Temperature=0, set Top-P=1 for fully deterministic output. Leave empty to use model default. Don't set both Temperature and Top-P to non-default simultaneously.">
              Top-P <span style={{ color: '#484f58' }}>ⓘ</span>
            </label>
            <input
              type="number"
              value={config.topP ?? ''}
              placeholder="default"
              min={0}
              max={1}
              step={0.05}
              onChange={e => {
                const v = e.target.value === '' ? null : parseFloat(e.target.value)
                update({ topP: v })
              }}
            />
          </div>
          <div>
            <label title="Fixed random seed for reproducible results. Same seed + same prompt = same output. Leave empty to disable. Supported by OpenAI and most local models (LM Studio).">
              Seed <span style={{ color: '#484f58' }}>ⓘ</span>
            </label>
            <input
              type="number"
              value={config.seed ?? ''}
              placeholder="disabled"
              min={0}
              step={1}
              onChange={e => {
                const v = e.target.value === '' ? null : parseInt(e.target.value)
                update({ seed: v })
              }}
            />
          </div>
        </div>

        <div style={{ marginTop: 6 }}>
          <label>
            Schema format <span style={{ color: '#484f58' }}>(sent in prompt)</span>
          </label>
          <div className="effort-row">
            {(['toon', 'json'] as InputFormat[]).map(fmt => (
              <button
                key={fmt}
                className={`effort-btn${config.inputFormat === fmt ? ' active' : ''}`}
                data-v={fmt}
                onClick={() => update({ inputFormat: fmt })}
              >
                {fmt.charAt(0).toUpperCase() + fmt.slice(1)}
              </button>
            ))}
          </div>
        </div>

        <div style={{ marginTop: 10 }}>
          <label>
            Output directory <span style={{ color: '#484f58' }}>(auto-save generated files)</span>
          </label>
          <input
            type="text"
            value={config.outputBaseDir}
            placeholder="e.g. C:/repo/McProtoNet/src/McProtoNet.Protocol/Packets"
            style={{ fontSize: 11 }}
            onChange={e => update({ outputBaseDir: e.target.value })}
          />
        </div>

        <div style={{ marginTop: 10, display: 'flex', alignItems: 'center', gap: 8 }}>
          <input
            id="dynamic-context"
            type="checkbox"
            checked={config.dynamicContext ?? true}
            onChange={e => update({ dynamicContext: e.target.checked })}
            style={{ cursor: 'pointer' }}
          />
          <label htmlFor="dynamic-context" style={{ cursor: 'pointer', userSelect: 'none' }}>
            Dynamic context{' '}
            <span style={{ color: '#484f58' }}>(only relevant IO methods in prompt)</span>
          </label>
        </div>
      </div>

      <AssessorSection />
      <PresetsSection focusedTier={focusedTier} />
    </div>
  )
}
