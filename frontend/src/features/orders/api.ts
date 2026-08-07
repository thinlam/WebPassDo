import { apiClient } from '../../api/client'
import type {
  ApiResponse,
  DeliverySpeed,
  HandOverPayload,
  OrderDetail,
  OrderListItem,
  OrderPreview,
  OrderStatus,
  PagedResult,
  PaymentMethod,
} from '../../types'
import { unwrap } from '../../utils/api'

export const ordersApi = {
  preview: (payload: {
    productId: string
    quantity: number
    shippingAddressId?: string | null
    deliverySpeed: DeliverySpeed
    paymentMethod: PaymentMethod
  }) => unwrap(apiClient.post<ApiResponse<OrderPreview>>('/orders/preview', payload)),

  create: (payload: {
    productId: string
    quantity: number
    shippingAddressId: string
    deliverySpeed: DeliverySpeed
    paymentMethod: PaymentMethod
    note?: string
  }) => unwrap(apiClient.post<ApiResponse<OrderDetail>>('/orders', payload)),

  myPurchases: (params: { page?: number; pageSize?: number; status?: OrderStatus } = {}) =>
    unwrap(
      apiClient.get<ApiResponse<PagedResult<OrderListItem>>>('/orders/my-purchases', { params }),
    ),

  mySales: (params: { page?: number; pageSize?: number; status?: OrderStatus } = {}) =>
    unwrap(apiClient.get<ApiResponse<PagedResult<OrderListItem>>>('/orders/my-sales', { params })),

  getById: (id: string) =>
    unwrap(apiClient.get<ApiResponse<OrderDetail>>(`/orders/${id}`)),

  paymentProof: (id: string, proofImageUrl: string) =>
    unwrap(
      apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/payment-proof`, { proofImageUrl }),
    ),

  confirmPayment: (id: string, note?: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/confirm-payment`, { note })),

  confirm: (id: string, note?: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/confirm`, { note })),

  reject: (id: string, reason: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/reject`, { reason })),

  cancel: (id: string, reason?: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/cancel`, { reason })),

  markPrepared: (id: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/mark-prepared`)),

  handOver: (id: string, payload: HandOverPayload) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/hand-over`, payload)),

  confirmDelivered: (id: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/confirm-delivered`)),

  complete: (id: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/complete`)),

  failDelivery: (id: string, reason: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/fail-delivery`, { reason })),
}
