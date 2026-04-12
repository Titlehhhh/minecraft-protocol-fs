import { create } from 'zustand'

interface UIStore {
  mainTab: 'code' | 'prompt'
  sidebarTab: 'packets' | 'config'
  setMainTab: (tab: 'code' | 'prompt') => void
  setSidebarTab: (tab: 'packets' | 'config') => void
}

export const useUIStore = create<UIStore>(set => ({
  mainTab: 'code',
  sidebarTab: 'packets',
  setMainTab: tab => set({ mainTab: tab }),
  setSidebarTab: tab => set({ sidebarTab: tab }),
}))
