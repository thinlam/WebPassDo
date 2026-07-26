import {
  getPasswordChecks,
  getPasswordStrength,
  PASSWORD_STRENGTH_LABEL,
} from '../../lib/passwordStrength'

const LABELS: { key: keyof ReturnType<typeof getPasswordChecks>; label: string }[] = [
  { key: 'minLength', label: 'Ít nhất 8 ký tự' },
  { key: 'hasUpper', label: 'Có chữ hoa' },
  { key: 'hasLower', label: 'Có chữ thường' },
  { key: 'hasDigit', label: 'Có chữ số' },
  { key: 'hasSpecial', label: 'Có ký tự đặc biệt (!@#$%^&*)' },
]

export function PasswordStrengthMeter({ password }: { password: string }) {
  const checks = getPasswordChecks(password)
  const strength = getPasswordStrength(password)
  const barClass =
    strength === 'strong' ? 'bg-emerald-600 w-full' : strength === 'medium' ? 'bg-amber-500 w-2/3' : 'bg-rose-600 w-1/3'

  if (!password) return null

  return (
    <div className="space-y-2 rounded-lg border border-line bg-sand/40 p-3">
      <div className="flex items-center justify-between text-xs">
        <span className="text-muted">Độ mạnh mật khẩu</span>
        <span className="font-medium text-ink">{PASSWORD_STRENGTH_LABEL[strength]}</span>
      </div>
      <div className="h-1.5 overflow-hidden rounded-full bg-line">
        <div className={`h-full transition-all ${barClass}`} />
      </div>
      <ul className="space-y-1 text-xs">
        {LABELS.map((item) => (
          <li key={item.key} className={checks[item.key] ? 'text-emerald-700' : 'text-rose-700'}>
            {checks[item.key] ? '✓' : '✕'} {item.label}
          </li>
        ))}
      </ul>
    </div>
  )
}
