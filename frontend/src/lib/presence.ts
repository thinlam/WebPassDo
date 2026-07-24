import { parseApiDate } from './datetime'

export function formatLastActive(
  lastSeenAt: string | null | undefined,
  now: Date = new Date(),
): string | null {
  if (!lastSeenAt) return null
  const seen = parseApiDate(lastSeenAt)
  if (Number.isNaN(seen.getTime())) return null

  const deltaMs = Math.max(0, now.getTime() - seen.getTime())
  const minutes = Math.floor(deltaMs / (60 * 1000))
  if (minutes < 60) {
    return `Hoạt động ${Math.max(1, minutes)} phút trước`
  }

  const hours = Math.floor(deltaMs / (60 * 60 * 1000))
  if (hours < 24) {
    return `Hoạt động ${Math.max(1, hours)} giờ trước`
  }

  const days = Math.floor(deltaMs / (24 * 60 * 60 * 1000))
  return `Hoạt động ${Math.max(1, days)} ngày trước`
}

export function isOnline(
  lastSeenAt: string | null | undefined,
  now: Date = new Date(),
  thresholdMs = 45000,
): boolean {
  if (!lastSeenAt) return false
  const seen = parseApiDate(lastSeenAt)
  if (Number.isNaN(seen.getTime())) return false
  return now.getTime() - seen.getTime() < thresholdMs
}

