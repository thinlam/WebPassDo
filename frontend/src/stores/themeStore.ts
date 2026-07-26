import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type ThemePreference = 'light' | 'dark' | 'system'

type ThemeState = {
  preference: ThemePreference
  setPreference: (preference: ThemePreference) => void
}

function resolveTheme(preference: ThemePreference): 'light' | 'dark' {
  if (preference === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  return preference
}

export function applyTheme(preference: ThemePreference) {
  const resolved = resolveTheme(preference)
  document.documentElement.setAttribute('data-theme', resolved)
  document.documentElement.style.colorScheme = resolved
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      preference: 'system',
      setPreference: (preference) => {
        applyTheme(preference)
        set({ preference })
      },
    }),
    {
      name: 'passdo-theme',
      onRehydrateStorage: () => (state) => {
        if (state) applyTheme(state.preference)
      },
    },
  ),
)
