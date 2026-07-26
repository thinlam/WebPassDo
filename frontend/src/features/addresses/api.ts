import { apiClient } from '../../api/client'
import type { AddressType, ApiResponse, UserAddress } from '../../types'
import { unwrap } from '../../utils/api'

export type AddressPayload = {
  recipientName: string
  phoneNumber: string
  province: string
  district: string
  ward: string
  provinceCode?: string | null
  districtCode?: string | null
  wardCode?: string | null
  streetAddress: string
  note?: string
  addressType: AddressType
  isDefault: boolean
}

export const addressesApi = {
  list: () => unwrap(apiClient.get<ApiResponse<UserAddress[]>>('/me/addresses')),

  create: (payload: AddressPayload) =>
    unwrap(apiClient.post<ApiResponse<UserAddress>>('/me/addresses', payload)),

  update: (id: string, payload: AddressPayload) =>
    unwrap(apiClient.put<ApiResponse<UserAddress>>(`/me/addresses/${id}`, payload)),

  remove: (id: string) => unwrap(apiClient.delete<ApiResponse<unknown>>(`/me/addresses/${id}`)),

  setDefault: (id: string) =>
    unwrap(apiClient.put<ApiResponse<UserAddress>>(`/me/addresses/${id}/default`)),
}
