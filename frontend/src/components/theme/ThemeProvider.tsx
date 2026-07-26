import { useEffect } from 'react'
import { applyTheme, useThemeStore } from '../../stores/themeStore'

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const preference = useThemeStore((s) => s.preference)

  useEffect(() => {
    applyTheme(preference)

    if (preference !== 'system') return

    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => applyTheme('system')
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [preference])

  return <>{children}</>
}
