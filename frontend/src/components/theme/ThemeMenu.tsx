import { useEffect, useRef, useState } from 'react'
import { useThemeStore, type ThemePreference } from '../../stores/themeStore'

const OPTIONS: { value: ThemePreference; label: string }[] = [
  { value: 'light', label: 'Sáng' },
  { value: 'dark', label: 'Tối' },
  { value: 'system', label: 'Theo hệ thống' },
]

export function ThemeMenu({ embedded = false, onSelected }: { embedded?: boolean; onSelected?: () => void }) {
  const preference = useThemeStore((s) => s.preference)
  const setPreference = useThemeStore((s) => s.setPreference)
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (embedded) return
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [embedded])

  const list = (
    <div className={embedded ? 'px-2 py-1' : 'py-1'}>
      {embedded && (
        <p className="px-2 py-1 text-xs font-medium uppercase tracking-wide text-muted">Giao diện</p>
      )}
      {OPTIONS.map((opt) => (
        <button
          key={opt.value}
          type="button"
          onClick={() => {
            setPreference(opt.value)
            onSelected?.()
            setOpen(false)
          }}
          className={`flex w-full items-center justify-between rounded-md px-2 py-2 text-left text-sm transition hover:bg-sand ${
            preference === opt.value ? 'font-semibold text-forest' : 'text-ink'
          }`}
        >
          <span>{opt.label}</span>
          {preference === opt.value && <span>✓</span>}
        </button>
      ))}
    </div>
  )

  if (embedded) return list

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        aria-label="Chế độ giao diện"
        onClick={() => setOpen((v) => !v)}
        className="rounded-md p-2 text-muted transition hover:bg-sand hover:text-ink"
      >
        <ThemeIcon />
      </button>
      {open && (
        <div className="absolute right-0 top-full z-40 mt-1 w-48 rounded-xl border border-line bg-surface py-1 shadow-lg">
          {list}
        </div>
      )}
    </div>
  )
}

function ThemeIcon() {
  return (
    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
      />
    </svg>
  )
}
