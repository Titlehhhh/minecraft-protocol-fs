import { create } from 'zustand'

type MainTab = 'code' | 'prompt'
type SidebarTab = 'packets' | 'types' | 'native' | 'config'

interface UIStore {
  mainTab: MainTab
  sidebarTab: SidebarTab
  protocolTypes: string[]
  protocolTypesByKind: Record<string, string[]>
  typesLoaded: boolean
  selectedType: string | null
  expandedKinds: Set<string>
  nativeTypes: string[]
  nativeTypesLoaded: boolean
  setMainTab: (tab: MainTab) => void
  setSidebarTab: (tab: SidebarTab) => void
  setProtocolTypes: (types: string[]) => void
  setProtocolTypesByKind: (typesByKind: Record<string, string[]>) => void
  selectType: (typeId: string | null) => void
  toggleKindExpanded: (kind: string) => void
  setNativeTypes: (types: string[]) => void
}

export const useUIStore = create<UIStore>(set => ({
  mainTab: 'code',
  sidebarTab: 'packets',
  protocolTypes: [],
  protocolTypesByKind: {},
  typesLoaded: false,
  selectedType: null,
  expandedKinds: new Set(['container', 'bitflags', 'buffer', 'array', 'option']), // Default expanded
  nativeTypes: [],
  nativeTypesLoaded: false,
  setMainTab: tab => set({ mainTab: tab }),
  setSidebarTab: tab => set({ sidebarTab: tab }),
  setProtocolTypes: types => set({ protocolTypes: types, typesLoaded: true }),
  setProtocolTypesByKind: typesByKind => set({ protocolTypesByKind: typesByKind, typesLoaded: true }),
  selectType: typeId => set({ selectedType: typeId }),
  toggleKindExpanded: kind => set(state => {
    const newExpanded = new Set(state.expandedKinds)
    if (newExpanded.has(kind)) {
      newExpanded.delete(kind)
    } else {
      newExpanded.add(kind)
    }
    return { expandedKinds: newExpanded }
  }),
  setNativeTypes: types => set({ nativeTypes: types, nativeTypesLoaded: true }),
}))
