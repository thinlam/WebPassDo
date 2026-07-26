import { apiClient } from '../../api/client'
import type { ApiResponse, PagedResult } from '../../types'
import { unwrap } from '../../utils/api'

export type AppNotification = {
  id: string
  type: string
  title: string
  content: string
  relatedEntityId?: string | null
  relatedEntityType?: string | null
  actionUrl?: string | null
  isRead: boolean
  readAt?: string | null
  createdAt: string
}

export const notificationsApi = {
  list: (params: { page?: number; pageSize?: number } = {}) =>
    unwrap(apiClient.get<ApiResponse<PagedResult<AppNotification>>>('/notifications', { params })),

  unreadCount: () => unwrap(apiClient.get<ApiResponse<number>>('/notifications/unread-count')),

  markRead: (id: string) =>
    unwrap(apiClient.post<ApiResponse<unknown>>(`/notifications/${id}/read`)),

  markAllRead: () => unwrap(apiClient.post<ApiResponse<unknown>>('/notifications/read-all')),
}
