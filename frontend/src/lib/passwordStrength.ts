export type PasswordStrength = 'weak' | 'medium' | 'strong'

export type PasswordChecks = {
  minLength: boolean
  hasUpper: boolean
  hasLower: boolean
  hasDigit: boolean
  hasSpecial: boolean
}

const SPECIAL = /[!@#$%^&*]/

export function getPasswordChecks(password: string): PasswordChecks {
  return {
    minLength: password.length >= 8,
    hasUpper: /[A-Z]/.test(password),
    hasLower: /[a-z]/.test(password),
    hasDigit: /\d/.test(password),
    hasSpecial: SPECIAL.test(password),
  }
}

export function getPasswordStrength(password: string): PasswordStrength {
  const checks = getPasswordChecks(password)
  const score = Object.values(checks).filter(Boolean).length
  if (score <= 2) return 'weak'
  if (score <= 4) return 'medium'
  return 'strong'
}

export function isPasswordStrong(password: string): boolean {
  const c = getPasswordChecks(password)
  return c.minLength && c.hasUpper && c.hasLower && c.hasDigit && c.hasSpecial
}

export const PASSWORD_STRENGTH_LABEL: Record<PasswordStrength, string> = {
  weak: 'Yếu',
  medium: 'Trung bình',
  strong: 'Mạnh',
}
