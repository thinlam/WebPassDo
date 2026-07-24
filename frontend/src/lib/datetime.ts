/** Parse API timestamps. Backend stores UTC; bare ISO without Z is treated as UTC. */
export function parseApiDate(value: string): Date {
  const s = value.trim()
  if (!s) return new Date(Number.NaN)
  if (/[zZ]$/.test(s) || /[+-]\d{2}:\d{2}$/.test(s)) {
    return new Date(s)
  }
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
    return new Date(`${s}T00:00:00Z`)
  }
  return new Date(`${s}Z`)
}
