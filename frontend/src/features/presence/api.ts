import { apiClient } from '../../api/client'
import type { ApiResponse } from '../../types'
import { unwrap } from '../../utils/api'

export type PresenceDto = {
  isOnline: boolean
  lastSeenAt?: string | null
}

export const presenceApi = {
  getUserPresence: (userId: string) =>
    unwrap(apiClient.get<ApiResponse<PresenceDto>>(`/users/${encodeURIComponent(userId)}/presence`)),
}

