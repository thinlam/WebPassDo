import type { ReactNode } from 'react'

type Props = {
  children: ReactNode
  className?: string
}

export function Button({
  children,
  className = '',
  variant = 'primary',
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
}) {
  const styles = {
    primary: 'bg-forest text-white hover:bg-forest-dark',
    secondary: 'bg-sand text-ink hover:bg-sand-dark border border-line',
    ghost: 'bg-transparent text-ink hover:bg-sand',
    danger: 'bg-rose-700 text-white hover:bg-rose-800',
  }[variant]

  return (
    <button
      className={`inline-flex items-center justify-center rounded-md px-4 py-2 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-50 ${styles} ${className}`}
      {...props}
    >
      {children}
    </button>
  )
}

export function Input({
  label,
  error,
  className = '',
  ...props
}: React.InputHTMLAttributes<HTMLInputElement> & { label?: string; error?: string }) {
  return (
    <label className="block space-y-1.5">
      {label && <span className="text-sm font-medium text-ink">{label}</span>}
      <input
        className={`w-full rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink outline-none ring-forest/30 focus:ring-2 ${className}`}
        {...props}
      />
      {error && <span className="text-xs text-rose-700">{error}</span>}
    </label>
  )
}

export function TextArea({
  label,
  error,
  className = '',
  ...props
}: React.TextareaHTMLAttributes<HTMLTextAreaElement> & { label?: string; error?: string }) {
  return (
    <label className="block space-y-1.5">
      {label && <span className="text-sm font-medium text-ink">{label}</span>}
      <textarea
        className={`w-full rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink outline-none ring-forest/30 focus:ring-2 ${className}`}
        {...props}
      />
      {error && <span className="text-xs text-rose-700">{error}</span>}
    </label>
  )
}

export function Select({
  label,
  error,
  children,
  className = '',
  ...props
}: React.SelectHTMLAttributes<HTMLSelectElement> & {
  label?: string
  error?: string
  children: ReactNode
}) {
  return (
    <label className="block space-y-1.5">
      {label && <span className="text-sm font-medium text-ink">{label}</span>}
      <select
        className={`w-full rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink outline-none ring-forest/30 focus:ring-2 ${className}`}
        {...props}
      >
        {children}
      </select>
      {error && <span className="text-xs text-rose-700">{error}</span>}
    </label>
  )
}

export function PageHeader({ title, description, actions }: { title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="font-display text-3xl tracking-tight text-ink md:text-4xl">{title}</h1>
        {description && <p className="mt-2 max-w-2xl text-sm text-muted md:text-base">{description}</p>}
      </div>
      {actions}
    </div>
  )
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="rounded-xl border border-dashed border-line bg-surface/70 px-6 py-16 text-center">
      <p className="font-display text-xl text-ink">{title}</p>
      {description && <p className="mt-2 text-sm text-muted">{description}</p>}
    </div>
  )
}

export function Spinner() {
  return (
    <div className="flex justify-center py-16">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-forest border-t-transparent" />
    </div>
  )
}

export function Badge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'neutral' | 'success' | 'warn' | 'danger' }) {
  const styles = {
    neutral: 'bg-sand text-ink',
    success: 'bg-emerald-500/15 text-emerald-800 dark:text-emerald-200',
    warn: 'bg-amber-500/15 text-amber-900 dark:text-amber-200',
    danger: 'bg-rose-500/15 text-rose-900 dark:text-rose-200',
  }[tone]
  return <span className={`inline-flex rounded-md px-2 py-0.5 text-xs font-medium ${styles}`}>{children}</span>
}

export function Section({ children, className = '' }: Props) {
  return <section className={`mx-auto w-full max-w-6xl px-4 py-8 md:px-6 ${className}`}>{children}</section>
}
