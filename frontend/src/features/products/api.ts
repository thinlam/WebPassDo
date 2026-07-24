import { apiClient } from '../../api/client'
import type {
  AcceptedPaymentOption,
  ApiResponse,
  DeliverySpeed,
  PagedResult,
  Product,
  ProductCondition,
  ProductFilters,
  ProductImage,
  ProductListItem,
  ProductStatus,
} from '../../types'
import { unwrap } from '../../utils/api'

export const productsApi = {
  list: (filters: ProductFilters = {}) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<ProductListItem>>>('/products', { params: filters }),
    ),

  myProducts: (params: { page?: number; pageSize?: number; status?: ProductStatus } = {}) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<ProductListItem>>>('/products/my-products', {
        params,
      }),
    ),

  getById: (id: string) => unwrap(apiClient.get<ApiResponse<Product>>(`/products/${id}`)),

  create: (payload: {
    name: string
    description: string
    originalPrice: number
    sellingPrice: number
    condition: ProductCondition | string
    categoryId: string
    location: string
    quantity?: number
    pickupAddressId?: string | null
    bankAccountId?: string | null
    acceptedPaymentOption?: AcceptedPaymentOption
    allowedDeliverySpeeds?: DeliverySpeed[]
    status?: string
  }) => unwrap(apiClient.post<ApiResponse<Product>>('/products', payload)),

  update: (
    id: string,
    payload: {
      name: string
      description: string
      originalPrice: number
      sellingPrice: number
      condition: ProductCondition | string
      categoryId: string
      location: string
      quantity: number
      pickupAddressId?: string | null
      bankAccountId?: string | null
      acceptedPaymentOption: AcceptedPaymentOption | string
      allowedDeliverySpeeds: (DeliverySpeed | string)[]
      status?: string
    },
  ) => unwrap(apiClient.put<ApiResponse<Product>>(`/products/${id}`, payload)),

  remove: (id: string) => unwrap(apiClient.delete<ApiResponse<unknown>>(`/products/${id}`)),

  updateStatus: (id: string, status: ProductStatus) =>
    unwrap(apiClient.patch<ApiResponse<Product>>(`/products/${id}/status`, { status })),

  uploadImage: async (productId: string, file: File, setAsPrimary = false) => {
    const form = new FormData()
    form.append('file', file)
    form.append('setAsPrimary', String(setAsPrimary))
    const { data } = await apiClient.post<ApiResponse<ProductImage>>(
      `/products/${productId}/images`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    )
    if (!data.success) throw new Error(data.message ?? 'Upload failed')
    return data.data
  },

  deleteImage: (productId: string, imageId: string) =>
    unwrap(apiClient.delete<ApiResponse<unknown>>(`/products/${productId}/images/${imageId}`)),

  setPrimaryImage: (productId: string, imageId: string) =>
    unwrap(
      apiClient.patch<ApiResponse<ProductImage>>(
        `/products/${productId}/images/${imageId}/primary`,
      ),
    ),
}
