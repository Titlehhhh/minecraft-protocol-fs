import { memo } from 'react'
import type { Tier } from '../../types'

interface Props {
  id: string
  tier?: Tier
  score?: number
  isSelected: boolean
  isChecked: boolean
  onSelect: (id: string) => void
  onCheck: (id: string, checked: boolean) => void
}

export const PacketItem = memo(function PacketItem({
  id, tier, score, isSelected, isChecked, onSelect, onCheck,
}: Props) {
  const cls = ['packet-item', isSelected ? 'selected' : '', isChecked ? 'checked' : '']
    .filter(Boolean)
    .join(' ')

  return (
    <div className={cls} data-id={id} title={id} onClick={() => onSelect(id)}>
      <input
        type="checkbox"
        className="pchk"
        checked={isChecked}
        onClick={e => e.stopPropagation()}
        onChange={e => onCheck(id, e.target.checked)}
      />
      <span className={`packet-tier-dot ${tier ?? 'unknown'}`} title={score != null ? `complexity ${score}` : undefined} />
      {id}
      {score != null && <span className="packet-item-score">{score}</span>}
    </div>
  )
})
