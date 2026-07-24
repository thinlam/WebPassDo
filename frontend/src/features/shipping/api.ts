import { apiClient } from '../../api/client'
import type { ApiResponse } from '../../types'
import { unwrap } from '../../utils/api'

export type ShippingCalculation = {
  fee: number
  estimatedDays?: number
  note?: string
}

export const shippingApi = {
  calculate: (payload: {
    productId: string
    shippingAddressId: string
    deliverySpeed: string
  }) => unwrap(apiClient.post<ApiResponse<ShippingCalculation>>('/shipping/calculate', payload)),
}
