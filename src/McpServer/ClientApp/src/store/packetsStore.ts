import { create } from 'zustand'
import { fetchPackets, fetchStats } from '../api/packets'
import type { PacketStat, Tier, TierFilter } from '../types'

export interface ComplexityInfo {
  score: number
  tier: Tier
}

interface PacketsStore {
  allPackets: string[]
  statsPackets: PacketStat[]
  complexityCache: Record<string, ComplexityInfo>
  tierFilter: TierFilter
  searchQuery: string
  selectedId: string
  checkedIds: Set<string>
  tierCounts: Partial<Record<Tier, number>>
  totalCount: number

  loadPackets: () => Promise<void>
  loadStats: () => Promise<void>
  setTierFilter: (tier: TierFilter) => void
  setSearchQuery: (q: string) => void
  selectPacket: (id: string) => void
  toggleCheck: (id: string, checked: boolean) => void
  checkAll: (ids: string[]) => void
  clearChecked: () => void
  cacheComplexity: (id: string, info: ComplexityInfo) => void
}

export const usePacketsStore = create<PacketsStore>((set, get) => ({
  allPackets: [],
  statsPackets: [],
  complexityCache: {},
  tierFilter: 'all',
  searchQuery: '',
  selectedId: '',
  checkedIds: new Set(),
  tierCounts: {},
  totalCount: 0,

  async loadPackets() {
    try {
      const data = await fetchPackets()
      const all: string[] = []
      for (const [ns, names] of Object.entries(data))
        for (const name of names)
          all.push(`${ns}.${name}`)
      all.sort()
      set({ allPackets: all })
    } catch { /* ignore */ }
  },

  async loadStats() {
    try {
      const s = await fetchStats()
      const cache = { ...get().complexityCache }
      for (const p of s.packets ?? [])
        cache[p.id] = { score: p.score, tier: p.tier }
      const sorted = [...(s.packets ?? [])].sort((a, b) => b.score - a.score)
      set({
        complexityCache: cache,
        statsPackets: sorted,
        tierCounts: s.tiers,
        totalCount: s.total,
      })
    } catch { /* ignore */ }
  },

  setTierFilter: tier => set({ tierFilter: tier }),
  setSearchQuery: q => set({ searchQuery: q }),
  selectPacket: id => set({ selectedId: id }),

  toggleCheck(id, checked) {
    const next = new Set(get().checkedIds)
    if (checked) next.add(id); else next.delete(id)
    set({ checkedIds: next })
  },

  checkAll(ids) {
    const next = new Set(get().checkedIds)
    for (const id of ids) next.add(id)
    set({ checkedIds: next })
  },

  clearChecked: () => set({ checkedIds: new Set() }),

  cacheComplexity: (id, info) =>
    set(s => ({ complexityCache: { ...s.complexityCache, [id]: info } })),
}))
