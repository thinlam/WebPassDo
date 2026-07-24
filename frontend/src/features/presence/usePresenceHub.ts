import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import { useEffect, useMemo, useSyncExternalStore } from 'react'
import { useAuthStore } from '../../stores/authStore'

export type PresenceChangedEvent = {
  userId: string
  isOnline: boolean
  lastSeenAt: string
}

export type TypingEvent = {
  conversationId: string
  userId: string
}

type PresenceListener = (evt: PresenceChangedEvent) => void
type TypingListener = (evt: TypingEvent) => void

export type PresenceHubActions = {
  connectionState: HubConnectionState
  joinConversation: (conversationId: string) => Promise<void>
  leaveConversation: (conversationId: string) => Promise<void>
  startTyping: (conversationId: string) => Promise<void>
  stopTyping: (conversationId: string) => Promise<void>
  subscribePresence: (listener: PresenceListener) => () => void
  subscribeTyping: (listener: { started?: TypingListener; stopped?: TypingListener }) => () => void
}

type Shared = {
  connection: HubConnection | null
  startPromise: Promise<void> | null
  heartbeatId: number | null
  refCount: number
  presenceListeners: Set<PresenceListener>
  typingListeners: {
    started: Set<TypingListener>
    stopped: Set<TypingListener>
  }
  stateListeners: Set<() => void>
}

const shared: Shared = {
  connection: null,
  startPromise: null,
  heartbeatId: null,
  refCount: 0,
  presenceListeners: new Set(),
  typingListeners: { started: new Set(), stopped: new Set() },
  stateListeners: new Set(),
}

function notifyState() {
  for (const cb of shared.stateListeners) cb()
}

function buildConnection() {
  const conn = new HubConnectionBuilder()
    .withUrl('/hubs/presence', {
      accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build()

  conn.on('PresenceChanged', (evt: PresenceChangedEvent) => {
    for (const cb of shared.presenceListeners) cb(evt)
  })
  conn.on('TypingStarted', (evt: TypingEvent) => {
    for (const cb of shared.typingListeners.started) cb(evt)
  })
  conn.on('TypingStopped', (evt: TypingEvent) => {
    for (const cb of shared.typingListeners.stopped) cb(evt)
  })

  conn.onreconnecting(() => notifyState())
  conn.onreconnected(() => notifyState())
  conn.onclose(() => notifyState())

  return conn
}

async function ensureStarted() {
  if (shared.startPromise) return shared.startPromise
  if (!shared.connection) {
    shared.connection = buildConnection()
  }
  notifyState()

  shared.startPromise = shared.connection
    .start()
    .then(() => {
      notifyState()
      if (shared.heartbeatId) {
        window.clearInterval(shared.heartbeatId)
        shared.heartbeatId = null
      }
      shared.heartbeatId = window.setInterval(() => {
        shared.connection?.invoke('Heartbeat').catch(() => {
          // ignore — reconnect loop handles
        })
      }, 20000)
    })
    .catch((err) => {
      shared.startPromise = null
      notifyState()
      throw err
    })

  return shared.startPromise
}

async function stopIfUnused() {
  if (shared.refCount > 0) return
  if (shared.heartbeatId) {
    window.clearInterval(shared.heartbeatId)
    shared.heartbeatId = null
  }
  const conn = shared.connection
  shared.connection = null
  shared.startPromise = null
  notifyState()
  if (conn && conn.state !== HubConnectionState.Disconnected) {
    try {
      await conn.stop()
    } catch {
      // ignore
    }
  }
}

export function usePresenceHub() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const hasToken = useAuthStore((s) => !!s.accessToken)

  useEffect(() => {
    if (!isAuthenticated || !hasToken) return
    shared.refCount += 1
    void ensureStarted()
    return () => {
      shared.refCount = Math.max(0, shared.refCount - 1)
      void stopIfUnused()
    }
  }, [isAuthenticated, hasToken])

  useEffect(() => {
    if (isAuthenticated) return
    shared.refCount = 0
    void stopIfUnused()
  }, [isAuthenticated])

  const connectionState = useSyncExternalStore(
    (onStoreChange) => {
      shared.stateListeners.add(onStoreChange)
      return () => {
        shared.stateListeners.delete(onStoreChange)
      }
    },
    () => shared.connection?.state ?? HubConnectionState.Disconnected,
    () => HubConnectionState.Disconnected,
  )

  const actions = useMemo<PresenceHubActions>(() => {
    return {
      connectionState,
      joinConversation: async (conversationId: string) => {
        if (!shared.connection) return
        await shared.connection.invoke('JoinConversation', conversationId)
      },
      leaveConversation: async (conversationId: string) => {
        if (!shared.connection) return
        await shared.connection.invoke('LeaveConversation', conversationId)
      },
      startTyping: async (conversationId: string) => {
        if (!shared.connection) return
        await shared.connection.invoke('StartTyping', conversationId)
      },
      stopTyping: async (conversationId: string) => {
        if (!shared.connection) return
        await shared.connection.invoke('StopTyping', conversationId)
      },
      subscribePresence: (listener: PresenceListener) => {
        shared.presenceListeners.add(listener)
        return () => {
          shared.presenceListeners.delete(listener)
        }
      },
      subscribeTyping: (listener: { started?: TypingListener; stopped?: TypingListener }) => {
        if (listener.started) shared.typingListeners.started.add(listener.started)
        if (listener.stopped) shared.typingListeners.stopped.add(listener.stopped)
        return () => {
          if (listener.started) shared.typingListeners.started.delete(listener.started)
          if (listener.stopped) shared.typingListeners.stopped.delete(listener.stopped)
        }
      },
    }
  }, [connectionState])

  return actions
}

