import { create } from 'zustand'

type MainTab = 'code' | 'prompt' | 'types'

interface UIStore {
  mainTab: MainTab
  sidebarTab: 'packets' | 'config'
  protocolTypes: string[]
  typesLoaded: boolean
  setMainTab: (tab: MainTab) => void
  setSidebarTab: (tab: 'packets' | 'config') => void
  setProtocolTypes: (types: string[]) => void
}

export const useUIStore = create<UIStore>(set => ({
  mainTab: 'code',
  sidebarTab: 'packets',
  protocolTypes: [],
  typesLoaded: false,
  setMainTab: tab => set({ mainTab: tab }),
  setSidebarTab: tab => set({ sidebarTab: tab }),
  setProtocolTypes: types => set({ protocolTypes: types, typesLoaded: true }),
}))
