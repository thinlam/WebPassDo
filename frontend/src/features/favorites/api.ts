import { apiClient } from '../../api/client'
import type { ApiResponse, Favorite, PagedResult } from '../../types'
import { unwrap } from '../../utils/api'

export const favoritesApi = {
  list: (page = 1, pageSize = 20) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<Favorite>>>('/favorites', {
        params: { page, pageSize },
      }),
    ),

  add: (productId: string) =>
    unwrap(apiClient.post<ApiResponse<Favorite>>(`/favorites/${productId}`)),

  remove: (productId: string) =>
    unwrap(apiClient.delete<ApiResponse<unknown>>(`/favorites/${productId}`)),
}
