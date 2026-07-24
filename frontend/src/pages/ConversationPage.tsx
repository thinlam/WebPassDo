import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnectionState } from '@microsoft/signalr'
import { chatApi } from '../features/chat/api'
import { useAuthStore } from '../stores/authStore'
import { Button, EmptyState, Section, Spinner } from '../components/common/ui'
import { formatDate } from '../lib/orderStatus'
import { PresenceLabel } from '../components/presence/PresenceLabel'
import { usePresenceHub } from '../features/presence/usePresenceHub'

export function ConversationPage() {
  const { conversationId = '' } = useParams()
  const user = useAuthStore((s) => s.user)
  const queryClient = useQueryClient()
  const [text, setText] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)
  const peerTypingClearId = useRef<number | null>(null)
  const localTypingDebounceId = useRef<number | null>(null)
  const localTypingIdleId = useRef<number | null>(null)
  const localTypingStarted = useRef(false)

  const {
    connectionState,
    joinConversation,
    leaveConversation,
    startTyping,
    stopTyping,
    subscribePresence,
    subscribeTyping,
  } = usePresenceHub()

  const { data: conversations } = useQuery({
    queryKey: ['conversations'],
    queryFn: () => chatApi.listConversations(),
  })

  const conversation = conversations?.find((c) => c.id === conversationId)
  const otherUserId = conversation?.otherUserId
  const otherUserName = conversation?.otherUserName
  const [livePresence, setLivePresence] = useState<{ isOnline?: boolean; lastSeenAt?: string | null }>({})
  const [isPeerTyping, setIsPeerTyping] = useState(false)

  const messagesQuery = useQuery({
    queryKey: ['messages', conversationId],
    queryFn: () => chatApi.getMessages(conversationId, { page: 1, pageSize: 100 }),
    enabled: !!conversationId,
    refetchInterval: 5000,
  })

  const sendMutation = useMutation({
    mutationFn: (content: string) => chatApi.sendMessage(conversationId, content),
    onSuccess: () => {
      setText('')
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] })
      queryClient.invalidateQueries({ queryKey: ['conversations'] })
    },
  })

  const messages = messagesQuery.data ?? []

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages.length])

  useEffect(() => {
    setLivePresence({})
    setIsPeerTyping(false)
    if (peerTypingClearId.current) window.clearTimeout(peerTypingClearId.current)
    peerTypingClearId.current = null
  }, [conversationId, otherUserId])

  useEffect(() => {
    if (!conversationId) return
    if (connectionState !== HubConnectionState.Connected) return
    void (async () => {
      try {
        await joinConversation(conversationId)
      } catch {
        // ignore — connection may be reconnecting
      }
    })()
    return () => {
      void (async () => {
        try {
          await leaveConversation(conversationId)
        } catch {
          // ignore
        }
      })()
    }
  }, [conversationId, connectionState, joinConversation, leaveConversation])

  useEffect(() => {
    if (!otherUserId) return
    const unsubscribe = subscribePresence((evt) => {
      if (evt.userId !== otherUserId) return
      setLivePresence({ isOnline: evt.isOnline, lastSeenAt: evt.lastSeenAt })
    })
    return () => {
      unsubscribe()
    }
  }, [otherUserId, subscribePresence])

  useEffect(() => {
    if (!conversationId || !otherUserId) return
    const unsubscribe = subscribeTyping({
      started: (evt) => {
        if (evt.conversationId !== conversationId) return
        if (evt.userId !== otherUserId) return
        setIsPeerTyping(true)
        if (peerTypingClearId.current) window.clearTimeout(peerTypingClearId.current)
        peerTypingClearId.current = window.setTimeout(() => setIsPeerTyping(false), 3000)
      },
      stopped: (evt) => {
        if (evt.conversationId !== conversationId) return
        if (evt.userId !== otherUserId) return
        setIsPeerTyping(false)
        if (peerTypingClearId.current) window.clearTimeout(peerTypingClearId.current)
        peerTypingClearId.current = null
      },
    })
    return () => {
      unsubscribe()
    }
  }, [conversationId, otherUserId, subscribeTyping])

  const stopLocalTypingNow = useCallback(() => {
    if (!conversationId) return
    if (localTypingDebounceId.current) window.clearTimeout(localTypingDebounceId.current)
    if (localTypingIdleId.current) window.clearTimeout(localTypingIdleId.current)
    localTypingDebounceId.current = null
    localTypingIdleId.current = null
    if (!localTypingStarted.current) return
    localTypingStarted.current = false
    void (async () => {
      try {
        await stopTyping(conversationId)
      } catch {
        // ignore
      }
    })()
  }, [conversationId, stopTyping])

  useEffect(() => {
    return () => {
      if (peerTypingClearId.current) window.clearTimeout(peerTypingClearId.current)
      peerTypingClearId.current = null
      stopLocalTypingNow()
    }
  }, [stopLocalTypingNow])

  const handleSend = (e: React.FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    stopLocalTypingNow()
    sendMutation.mutate(text.trim())
  }

  if (!conversationId) return <EmptyState title="Không tìm thấy cuộc trò chuyện" />

  const isOnline = livePresence.isOnline ?? conversation?.otherUserIsOnline
  const lastSeenAt = livePresence.lastSeenAt ?? conversation?.otherUserLastSeenAt

  return (
    <Section className="max-w-2xl">
      <div className="flex h-[calc(100vh-200px)] flex-col rounded-2xl border border-line bg-white/80">
        <div className="border-b border-line px-4 py-3">
          <p className="font-semibold text-ink">{otherUserName ?? 'Cuộc trò chuyện'}</p>
          <PresenceLabel isOnline={isOnline} lastSeenAt={lastSeenAt} isTyping={isPeerTyping} />
        </div>
        <div className="flex-1 overflow-y-auto p-4 space-y-3">
          {messagesQuery.isLoading && <Spinner />}
          {!messagesQuery.isLoading && messages.length === 0 && (
            <p className="py-8 text-center text-sm text-muted">Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện!</p>
          )}
          {messages.map((msg) => {
            const isMine = msg.senderId === user?.id
            return (
              <div key={msg.id} className={`flex ${isMine ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[75%] rounded-2xl px-4 py-2 text-sm ${
                    isMine
                      ? 'bg-forest text-white'
                      : 'bg-sand text-ink'
                  }`}
                >
                  <p className="whitespace-pre-wrap">{msg.content}</p>
                  <p className={`mt-1 text-[10px] ${isMine ? 'text-white/70' : 'text-muted'}`}>
                    {formatDate(msg.createdAt)}
                  </p>
                </div>
              </div>
            )
          })}
          <div ref={bottomRef} />
        </div>

        <form onSubmit={handleSend} className="flex gap-2 border-t border-line p-3">
          <input
            type="text"
            value={text}
            onChange={(e) => {
              const value = e.target.value
              setText(value)
              if (!conversationId) return

              if (localTypingDebounceId.current) window.clearTimeout(localTypingDebounceId.current)
              localTypingDebounceId.current = window.setTimeout(() => {
                if (localTypingStarted.current) return
                localTypingStarted.current = true
                void (async () => {
                  try {
                    await startTyping(conversationId)
                  } catch {
                    // ignore
                  }
                })()
              }, 300)

              if (localTypingIdleId.current) window.clearTimeout(localTypingIdleId.current)
              localTypingIdleId.current = window.setTimeout(() => {
                stopLocalTypingNow()
              }, 2500)
            }}
            placeholder="Nhập tin nhắn..."
            className="flex-1 rounded-md border border-line bg-white px-3 py-2 text-sm outline-none ring-forest/30 focus:ring-2"
          />
          <Button type="submit" disabled={!text.trim() || sendMutation.isPending}>
            Gửi
          </Button>
        </form>
      </div>
    </Section>
  )
}
