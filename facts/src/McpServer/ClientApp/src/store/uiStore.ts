import { create } from 'zustand'
import type { BuildOrderResult } from '../api/packets'

type MainTab = 'code' | 'prompt'
type MainView = 'protocol' | 'workbench' | 'graph' | 'usage' | 'settings'
type SidebarTab = 'packets' | 'types' | 'native'
type TypeGrouping = 'kind' | 'buildOrder'
type SelectedOwner = { kind: 'packet' | 'type', id: string } | null

const DONE_TYPES_KEY = 'mcpn.doneTypes'

function loadDoneTypes(): Set<string> {
  try {
    const raw = localStorage.getItem(DONE_TYPES_KEY)
    if (!raw) return new Set()
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? new Set(parsed as string[]) : new Set()
  } catch {
    return new Set()
  }
}

function persistDoneTypes(done: Set<string>): void {
  try {
    localStorage.setItem(DONE_TYPES_KEY, JSON.stringify([...done]))
  } catch {
    /* ignore quota / privacy-mode errors */
  }
}

interface UIStore {
  mainTab: MainTab
  mainView: MainView
  sidebarTab: SidebarTab
  selectedOwner: SelectedOwner
  sourcePanelOpen: boolean
  protocolTypes: string[]
  protocolTypesByKind: Record<string, string[]>
  typesLoaded: boolean
  selectedType: string | null
  expandedKinds: Set<string>
  nativeTypes: string[]
  nativeTypesLoaded: boolean
  typeGrouping: TypeGrouping
  buildOrder: BuildOrderResult | null
  buildOrderLoaded: boolean
  doneTypes: Set<string>
  setMainTab: (tab: MainTab) => void
  setMainView: (view: MainView) => void
  setSidebarTab: (tab: SidebarTab) => void
  setSelectedOwner: (owner: SelectedOwner) => void
  setSourcePanelOpen: (open: boolean) => void
  setProtocolTypes: (types: string[]) => void
  setProtocolTypesByKind: (typesByKind: Record<string, string[]>) => void
  selectType: (typeId: string | null) => void
  toggleKindExpanded: (kind: string) => void
  setNativeTypes: (types: string[]) => void
  setTypeGrouping: (grouping: TypeGrouping) => void
  setBuildOrder: (buildOrder: BuildOrderResult) => void
  toggleTypeDone: (name: string) => void
}

export const useUIStore = create<UIStore>(set => ({
  mainTab: 'code',
  mainView: 'protocol',
  sidebarTab: 'packets',
  selectedOwner: null,
  sourcePanelOpen: true,
  protocolTypes: [],
  protocolTypesByKind: {},
  typesLoaded: false,
  selectedType: null,
  expandedKinds: new Set(['container', 'bitflags', 'buffer', 'array', 'option']), // Default expanded
  nativeTypes: [],
  nativeTypesLoaded: false,
  typeGrouping: 'kind',
  buildOrder: null,
  buildOrderLoaded: false,
  doneTypes: loadDoneTypes(),
  setMainTab: tab => set({ mainTab: tab }),
  setMainView: view => set({ mainView: view }),
  setSidebarTab: tab => set({ sidebarTab: tab }),
  setSelectedOwner: owner => set({ selectedOwner: owner }),
  setSourcePanelOpen: open => set({ sourcePanelOpen: open }),
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
  setTypeGrouping: grouping => set({ typeGrouping: grouping }),
  setBuildOrder: buildOrder => set({ buildOrder, buildOrderLoaded: true }),
  toggleTypeDone: name => set(state => {
    const done = new Set(state.doneTypes)
    if (done.has(name)) done.delete(name)
    else done.add(name)
    persistDoneTypes(done)
    return { doneTypes: done }
  }),
}))
