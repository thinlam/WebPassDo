import { apiClient } from '../../api/client'
import type { AddressType, ApiResponse, UserAddress } from '../../types'
import { unwrap } from '../../utils/api'

export const addressesApi = {
  list: () => unwrap(apiClient.get<ApiResponse<UserAddress[]>>('/me/addresses')),

  create: (payload: {
    recipientName: string
    phoneNumber: string
    province: string
    district: string
    ward: string
    streetAddress: string
    note?: string
    addressType: AddressType
    isDefault: boolean
  }) => unwrap(apiClient.post<ApiResponse<UserAddress>>('/me/addresses', payload)),

  update: (
    id: string,
    payload: {
      recipientName: string
      phoneNumber: string
      province: string
      district: string
      ward: string
      streetAddress: string
      note?: string
      addressType: AddressType
      isDefault: boolean
    },
  ) => unwrap(apiClient.put<ApiResponse<UserAddress>>(`/me/addresses/${id}`, payload)),

  remove: (id: string) => unwrap(apiClient.delete<ApiResponse<unknown>>(`/me/addresses/${id}`)),

  setDefault: (id: string) =>
    unwrap(apiClient.put<ApiResponse<UserAddress>>(`/me/addresses/${id}/default`)),
}
