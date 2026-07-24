import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { chatApi } from '../features/chat/api'
import { EmptyState, PageHeader, Section, Spinner } from '../components/common/ui'
import { formatDate } from '../lib/orderStatus'
import { PresenceLabel } from '../components/presence/PresenceLabel'
import { usePresenceHub } from '../features/presence/usePresenceHub'

export function MessagesPage() {
  const { subscribePresence } = usePresenceHub()
  const [presenceByUserId, setPresenceByUserId] = useState<
    Record<string, { isOnline: boolean; lastSeenAt: string }>
  >({})

  useEffect(() => {
    const unsubscribe = subscribePresence((evt) => {
      setPresenceByUserId((prev) => {
        const current = prev[evt.userId]
        if (current?.isOnline === evt.isOnline && current?.lastSeenAt === evt.lastSeenAt) return prev
        return { ...prev, [evt.userId]: { isOnline: evt.isOnline, lastSeenAt: evt.lastSeenAt } }
      })
    })
    return () => unsubscribe()
  }, [subscribePresence])

  const query = useQuery({
    queryKey: ['conversations'],
    queryFn: () => chatApi.listConversations(),
    refetchInterval: 10000,
  })

  return (
    <Section className="max-w-2xl">
      <PageHeader title="Tin nhắn" description="Trò chuyện với người mua / người bán." />
      {query.isLoading && <Spinner />}
      {query.data?.length === 0 && (
        <EmptyState title="Chưa có cuộc trò chuyện" description="Bắt đầu trò chuyện từ trang sản phẩm." />
      )}
      <div className="space-y-2">
        {query.data?.map((conv) => {
          const live = presenceByUserId[conv.otherUserId]
          const isOnline = live?.isOnline ?? conv.otherUserIsOnline
          const lastSeenAt = live?.lastSeenAt ?? conv.otherUserLastSeenAt
          const preview = conv.lastMessage ?? conv.lastMessagePreview

          return (
            <Link
              key={conv.id}
              to={`/messages/${conv.id}`}
              className="flex items-center gap-4 rounded-2xl border border-line bg-white/80 p-4 transition hover:-translate-y-0.5 hover:shadow-md"
            >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-forest/10 text-sm font-medium text-forest">
              {conv.otherUserName?.charAt(0)?.toUpperCase() || '?'}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <p className="truncate font-medium text-ink">{conv.otherUserName}</p>
                  <PresenceLabel isOnline={isOnline} lastSeenAt={lastSeenAt} />
                </div>
                {conv.lastMessageAt && (
                  <span className="shrink-0 text-xs text-muted">{formatDate(conv.lastMessageAt)}</span>
                )}
              </div>
              {preview && <p className="truncate text-sm text-muted">{preview}</p>}
            </div>
            {conv.unreadCount > 0 && (
              <span className="flex h-5 w-5 items-center justify-center rounded-full bg-forest text-[10px] font-bold text-white">
                {conv.unreadCount}
              </span>
            )}
            </Link>
          )
        })}
      </div>
    </Section>
  )
}
