import { apiClient } from '../../api/client'
import type { ApiResponse, AuthSession, AuthUser } from '../../types'
import { unwrap } from '../../utils/api'

export const authApi = {
  register: (payload: {
    email: string
    password: string
    fullName: string
    phoneNumber?: string
  }) => unwrap(apiClient.post<ApiResponse<AuthSession>>('/auth/register', payload)),

  login: (payload: { email: string; password: string }) =>
    unwrap(apiClient.post<ApiResponse<AuthSession>>('/auth/login', payload)),

  googleLogin: (idToken: string) =>
    unwrap(apiClient.post<ApiResponse<AuthSession>>('/auth/google', { idToken })),

  refreshToken: (refreshToken: string) =>
    unwrap(apiClient.post<ApiResponse<AuthSession>>('/auth/refresh-token', { refreshToken })),

  logout: (refreshToken: string) =>
    unwrap(apiClient.post<ApiResponse<unknown>>('/auth/logout', { refreshToken })),

  me: () => unwrap(apiClient.get<ApiResponse<AuthUser>>('/users/me')),

  updateMe: (payload: { fullName: string; phoneNumber?: string; avatarUrl?: string }) =>
    unwrap(apiClient.put<ApiResponse<AuthUser>>('/users/me', payload)),

  changePassword: (payload: { currentPassword: string; newPassword: string }) =>
    unwrap(apiClient.post<ApiResponse<unknown>>('/auth/change-password', payload)),
}
