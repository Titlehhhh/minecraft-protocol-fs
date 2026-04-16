import { create } from 'zustand'

type MainTab = 'code' | 'prompt'
type SidebarTab = 'packets' | 'types' | 'config'

interface UIStore {
  mainTab: MainTab
  sidebarTab: SidebarTab
  protocolTypes: string[]
  typesLoaded: boolean
  selectedType: string | null
  setMainTab: (tab: MainTab) => void
  setSidebarTab: (tab: SidebarTab) => void
  setProtocolTypes: (types: string[]) => void
  selectType: (typeId: string | null) => void
}

export const useUIStore = create<UIStore>(set => ({
  mainTab: 'code',
  sidebarTab: 'packets',
  protocolTypes: [],
  typesLoaded: false,
  selectedType: null,
  setMainTab: tab => set({ mainTab: tab }),
  setSidebarTab: tab => set({ sidebarTab: tab }),
  setProtocolTypes: types => set({ protocolTypes: types, typesLoaded: true }),
  selectType: typeId => set({ selectedType: typeId }),
}))
