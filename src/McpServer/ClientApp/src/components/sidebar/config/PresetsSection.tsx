import { useConfigStore } from '../../../store/configStore'
import type { Tier } from '../../../types'

const PRESETS = [
  'openai/gpt-4o-mini',
  'openai/gpt-oss-120b',
  'qwen/qwen3-235b-a22b-2507',
  'qwen/qwen3-30b-a3b',
  'qwen/qwen2.5-coder-32b-instruct',
  'deepseek/deepseek-r1-0528',
  'anthropic/claude-3-haiku',
  'anthropic/claude-3-5-sonnet',
  'openai/gpt-4o',
  'google/gemini-flash-2.0',
]

interface Props {
  focusedTier: Tier
}

export function PresetsSection({ focusedTier }: Props) {
  const update = useConfigStore(s => s.update)

  const apply = (model: string) => {
    update(prev => ({
      ...prev,
      [focusedTier]: { ...prev[focusedTier], model },
    }))
  }

  const shortLabel = (preset: string) => preset.split('/')[1] ?? preset

  return (
    <div className="sidebar-section">
      <h2>
        Presets <span className="preset-sub">→ фокус на поле → клик</span>
      </h2>
      <div className="presets">
        {PRESETS.map(p => (
          <span key={p} className="preset" title={p} onClick={() => apply(p)}>
            {shortLabel(p)}
          </span>
        ))}
      </div>
    </div>
  )
}
