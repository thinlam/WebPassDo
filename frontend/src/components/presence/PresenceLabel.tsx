import { useEffect, useState } from 'react'
import { formatLastActive, isOnline as isOnlineByLastSeenAt } from '../../lib/presence'

export function PresenceLabel({
  isOnline,
  lastSeenAt,
  isTyping,
}: {
  isOnline?: boolean
  lastSeenAt?: string | null
  isTyping?: boolean
}) {
  const [, setTick] = useState(0)
  useEffect(() => {
    const id = window.setInterval(() => setTick((t) => t + 1), 15000)
    return () => window.clearInterval(id)
  }, [])

  if (isTyping) return <p className="text-sm text-forest">Đang nhập...</p>

  const effectiveOnline = lastSeenAt
    ? isOnlineByLastSeenAt(lastSeenAt)
    : Boolean(isOnline)

  if (effectiveOnline) {
    return (
      <p className="flex items-center gap-1.5 text-sm text-forest">
        <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
        Đang online
      </p>
    )
  }

  const text = formatLastActive(lastSeenAt)
  if (!text) return null
  return <p className="text-sm text-muted">{text}</p>
}

