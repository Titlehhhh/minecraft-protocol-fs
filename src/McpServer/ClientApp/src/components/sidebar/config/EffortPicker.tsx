import type { ReasoningEffort } from '../../../types'

interface Option {
  value: ReasoningEffort
  label: string
}

const ALL_OPTIONS: Option[] = [
  { value: '',       label: 'Off'    },
  { value: 'low',    label: 'low'    },
  { value: 'medium', label: 'medium' },
  { value: 'high',   label: 'high'   },
  { value: 'xhigh',  label: 'xhigh'  },
]

interface Props {
  value: ReasoningEffort
  onChange: (v: ReasoningEffort) => void
  includeXHigh?: boolean
}

export function EffortPicker({ value, onChange, includeXHigh = true }: Props) {
  const options = includeXHigh ? ALL_OPTIONS : ALL_OPTIONS.slice(0, -1)
  return (
    <div className="effort-row">
      {options.map(opt => (
        <button
          key={opt.value}
          className={`effort-btn${value === opt.value ? ' active' : ''}`}
          data-v={opt.value}
          onClick={() => onChange(opt.value)}
        >
          {opt.label}
        </button>
      ))}
    </div>
  )
}
