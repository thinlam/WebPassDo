import { apiClient } from '../../api/client'
import type { ApiResponse, UserBankAccount } from '../../types'
import { unwrap } from '../../utils/api'

export const bankAccountsApi = {
  list: () => unwrap(apiClient.get<ApiResponse<UserBankAccount[]>>('/me/bank-accounts')),

  create: (payload: {
    bankName: string
    accountNumber: string
    accountHolderName: string
    branch?: string
    isDefault: boolean
  }) => unwrap(apiClient.post<ApiResponse<UserBankAccount>>('/me/bank-accounts', payload)),

  update: (
    id: string,
    payload: {
      bankName: string
      accountNumber: string
      accountHolderName: string
      branch?: string
      isDefault: boolean
    },
  ) => unwrap(apiClient.put<ApiResponse<UserBankAccount>>(`/me/bank-accounts/${id}`, payload)),

  remove: (id: string) =>
    unwrap(apiClient.delete<ApiResponse<unknown>>(`/me/bank-accounts/${id}`)),

  setDefault: (id: string) =>
    unwrap(apiClient.put<ApiResponse<UserBankAccount>>(`/me/bank-accounts/${id}/default`)),
}
