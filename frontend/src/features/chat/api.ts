import { apiClient } from '../../api/client'
import type { ApiResponse, Conversation, Message } from '../../types'
import { unwrap } from '../../utils/api'

export const chatApi = {
  listConversations: () =>
    unwrap(apiClient.get<ApiResponse<Conversation[]>>('/conversations')),

  getOrCreate: (productId: string) => {
    const id = productId?.trim()
    if (!id) {
      return Promise.reject(new Error('Thiếu mã sản phẩm.'))
    }
    // Route-based — no JSON body binding issues
    return unwrap(
      apiClient.post<ApiResponse<Conversation>>(`/conversations/product/${encodeURIComponent(id)}`),
    )
  },

  getMessages: (conversationId: string, params: { page?: number; pageSize?: number; after?: string } = {}) =>
    unwrap(
      apiClient.get<ApiResponse<Message[]>>(`/conversations/${conversationId}/messages`, {
        params,
      }),
    ),

  sendMessage: (conversationId: string, content: string) =>
    unwrap(
      apiClient.post<ApiResponse<Message>>(`/conversations/${conversationId}/messages`, { content }),
    ),

  markRead: (conversationId: string) =>
    unwrap(apiClient.post<ApiResponse<unknown>>(`/conversations/${conversationId}/read`)),
}
