import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { HubConnectionState } from '@microsoft/signalr'
import { usePresenceHub } from '../presence/usePresenceHub'
import type { AppNotification } from './api'

/** Bridges SignalR NotificationReceived → React Query cache refresh. */
export function NotificationRealtimeBridge() {
  const queryClient = useQueryClient()
  const { subscribeNotifications, connectionState } = usePresenceHub()

  useEffect(() => {
    return subscribeNotifications((raw) => {
      const notification = raw as AppNotification
      queryClient.setQueryData<number>(['notifications', 'unread-count'], (prev) => {
        if (notification?.isRead) return prev ?? 0
        return typeof prev === 'number' ? prev + 1 : 1
      })
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    })
  }, [subscribeNotifications, queryClient])

  // Fallback poll when socket is disconnected
  useEffect(() => {
    if (connectionState === HubConnectionState.Connected) return
    const id = window.setInterval(() => {
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] })
    }, 60_000)
    return () => window.clearInterval(id)
  }, [connectionState, queryClient])

  return null
}
