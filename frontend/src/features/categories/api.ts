import { apiClient } from '../../api/client'
import type { ApiResponse, Category } from '../../types'
import { unwrap } from '../../utils/api'

export const categoriesApi = {
  list: (includeInactive = false) =>
    unwrap(apiClient.get<ApiResponse<Category[]>>('/categories', { params: { includeInactive } })),

  getById: (id: string) => unwrap(apiClient.get<ApiResponse<Category>>(`/categories/${id}`)),

  create: (payload: {
    name: string
    description?: string
    slug?: string
    displayOrder?: number
    isActive?: boolean
  }) => unwrap(apiClient.post<ApiResponse<Category>>('/categories', payload)),

  update: (
    id: string,
    payload: {
      name: string
      description?: string
      slug?: string
      displayOrder: number
      isActive: boolean
    },
  ) => unwrap(apiClient.put<ApiResponse<Category>>(`/categories/${id}`, payload)),

  remove: (id: string) => unwrap(apiClient.delete<ApiResponse<unknown>>(`/categories/${id}`)),
}
