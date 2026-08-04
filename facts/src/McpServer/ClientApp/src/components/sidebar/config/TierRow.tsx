import { useState } from 'react'
import { useConfigStore } from '../../../store/configStore'
import { EffortPicker } from './EffortPicker'
import type { Tier, TierConfig, ModelConfig, ReasoningEffort } from '../../../types'

export interface TierRowDef {
  tier: Tier
  label: string
  thresholdField?: keyof ModelConfig
  thresholdPrefix?: string
  modelPlaceholder: string
}

export const TIER_ROW_DEFS: TierRowDef[] = [
  {
    tier: 'tiny',
    label: 'TINY',
    thresholdField: 'tinyComplexityThreshold',
    thresholdPrefix: 'complexity\u00a0≤\u00a0',
    modelPlaceholder: 'empty → fallback to Easy',
  },
  {
    tier: 'easy',
    label: 'EASY',
    thresholdField: 'easyComplexityThreshold',
    thresholdPrefix: 'complexity\u00a0≤\u00a0',
    modelPlaceholder: 'model id',
  },
  {
    tier: 'medium',
    label: 'MED',
    thresholdField: 'heavyComplexityThreshold',
    thresholdPrefix: '≤\u00a0',
    modelPlaceholder: 'model id',
  },
  {
    tier: 'heavy',
    label: 'HEAVY',
    modelPlaceholder: 'empty → return to Claude',
  },
]

interface Props {
  def: TierRowDef
  onFocusModel: (tier: Tier) => void
}

export function TierRow({ def, onFocusModel }: Props) {
  const [open, setOpen] = useState(false)
  const config = useConfigStore(s => s.config)
  const update = useConfigStore(s => s.update)

  const tierCfg: TierConfig = config[def.tier]

  const setTier = (patch: Partial<TierConfig>) =>
    update(prev => ({ ...prev, [def.tier]: { ...prev[def.tier], ...patch } }))

  const setThreshold = (value: number) =>
    def.thresholdField && update({ [def.thresholdField]: value } as Partial<ModelConfig>)

  const thresholdValue = def.thresholdField ? (config[def.thresholdField] as number) : undefined

  return (
    <>
      <div className="tier">
        <span className={`tier-tag ${def.tier}`}>{def.label}</span>
        <input
          type="text"
          value={tierCfg.model}
          placeholder={def.modelPlaceholder}
          onFocus={() => onFocusModel(def.tier)}
          onChange={e => setTier({ model: e.target.value })}
        />
        {def.thresholdField && (
          <span style={{ fontSize: 10, color: '#484f58', whiteSpace: 'nowrap' }}>
            {def.thresholdPrefix}
            <input
              type="number"
              className="sm"
              value={thresholdValue}
              min={0}
              onChange={e => setThreshold(Number(e.target.value))}
            />
          </span>
        )}
        <span
          className={`tier-adv-toggle${open ? ' open' : ''}`}
          onClick={() => setOpen(v => !v)}
        >
          ▶
        </span>
      </div>
      <div className={`tier-adv${open ? ' open' : ''}`}>
        <EffortPicker
          value={tierCfg.reasoningEffort as ReasoningEffort}
          onChange={v => setTier({ reasoningEffort: v })}
        />
        <input
          type="text"
          placeholder="endpoint (empty = OpenRouter, e.g. http://localhost:1234/v1)"
          value={tierCfg.endpoint}
          style={{ marginTop: 6 }}
          onChange={e => setTier({ endpoint: e.target.value })}
        />
        <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontSize: 11, color: '#484f58' }}>batch concurrency</span>
          <input
            type="number"
            className="sm"
            value={tierCfg.maxConcurrency}
            min={1}
            max={32}
            onChange={e => setTier({ maxConcurrency: Number(e.target.value) })}
          />
        </div>
      </div>
    </>
  )
}
