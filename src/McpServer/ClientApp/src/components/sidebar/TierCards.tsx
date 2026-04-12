import { usePacketsStore } from '../../store/packetsStore'
import type { TierFilter } from '../../types'

const CARDS: { tier: TierFilter; label: string }[] = [
  { tier: 'all',    label: 'ALL' },
  { tier: 'tiny',   label: 'TINY' },
  { tier: 'easy',   label: 'EASY' },
  { tier: 'medium', label: 'MED' },
  { tier: 'heavy',  label: 'HEAVY' },
]

const COUNT_IDS: Record<string, string> = {
  all: 'totalCount', tiny: 'tiny', easy: 'easy', medium: 'medium', heavy: 'heavy',
}

export function TierCards() {
  const { tierFilter, setTierFilter, tierCounts, totalCount } = usePacketsStore(s => ({
    tierFilter: s.tierFilter,
    setTierFilter: s.setTierFilter,
    tierCounts: s.tierCounts,
    totalCount: s.totalCount,
  }))

  const getCount = (tier: TierFilter) => {
    if (tier === 'all') return totalCount || '–'
    return tierCounts[tier as keyof typeof tierCounts] ?? '–'
  }

  return (
    <div className="stats-tier-cards">
      {CARDS.map(({ tier, label }) => (
        <div
          key={tier}
          className={`stats-card ${tier}${tierFilter === tier ? ' active' : ''}`}
          onClick={() => setTierFilter(tier)}
        >
          <span className="stats-card-count">{getCount(tier)}</span>
          <span className="stats-card-label">{label}</span>
        </div>
      ))}
    </div>
  )
}
