import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { notificationsApi, type AppNotification } from '../../features/notifications/api'
import { formatDate } from '../../lib/orderStatus'
import { getErrorMessage } from '../../utils/api'

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const countQuery = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => notificationsApi.unreadCount(),
    // Realtime pushes updates; keep a slow safety poll.
    refetchInterval: 120_000,
  })

  const listQuery = useQuery({
    queryKey: ['notifications', 'list'],
    queryFn: () => notificationsApi.list({ page: 1, pageSize: 20 }),
    enabled: open,
  })

  const markReadM = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })

  const markAllM = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  const unread = countQuery.data ?? 0
  const items = listQuery.data?.items ?? []

  const openNotification = async (n: AppNotification) => {
    if (!n.isRead) {
      try {
        await markReadM.mutateAsync(n.id)
      } catch {
        // still navigate
      }
    }
    setOpen(false)
    if (n.actionUrl) {
      navigate(n.actionUrl)
    } else if (n.relatedEntityType === 'Order' && n.relatedEntityId) {
      navigate(`/orders/${n.relatedEntityId}`)
    }
  }

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        aria-label="Thông báo"
        onClick={() => setOpen((v) => !v)}
        className="relative rounded-md p-2 text-muted transition hover:bg-sand hover:text-ink"
      >
        <BellIcon />
        {unread > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-600 px-1 text-[10px] font-semibold text-white">
            {unread > 99 ? '99+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full z-40 mt-2 w-[22rem] max-w-[90vw] overflow-hidden rounded-xl border border-line bg-surface shadow-xl">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <p className="font-medium text-ink">Thông báo</p>
            <button
              type="button"
              className="text-xs text-forest hover:underline disabled:opacity-50"
              disabled={markAllM.isPending || unread === 0}
              onClick={() => markAllM.mutate()}
            >
              Đánh dấu tất cả đã đọc
            </button>
          </div>

          <div className="max-h-96 overflow-y-auto">
            {listQuery.isLoading && (
              <p className="px-4 py-8 text-center text-sm text-muted">Đang tải...</p>
            )}
            {listQuery.isError && (
              <p className="px-4 py-8 text-center text-sm text-rose-700">
                {getErrorMessage(listQuery.error)}
              </p>
            )}
            {!listQuery.isLoading && items.length === 0 && (
              <p className="px-4 py-10 text-center text-sm text-muted">Chưa có thông báo nào</p>
            )}
            {items.map((n) => (
              <button
                key={n.id}
                type="button"
                onClick={() => openNotification(n)}
                className={`block w-full border-b border-line px-4 py-3 text-left transition hover:bg-sand/60 ${
                  n.isRead ? 'bg-surface' : 'bg-sand/40'
                }`}
              >
                <p className={`text-sm ${n.isRead ? 'text-ink' : 'font-semibold text-ink'}`}>
                  {n.title}
                </p>
                <p className="mt-1 line-clamp-2 text-xs text-muted">{n.content}</p>
                <p className="mt-1 text-[11px] text-muted">{formatDate(n.createdAt)}</p>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function BellIcon() {
  return (
    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
      />
    </svg>
  )
}
