import type { ApiResponse } from '../types'

export function getErrorMessage(error: unknown, fallback = 'Đã xảy ra lỗi.') {
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const response = (error as { response?: { data?: ApiResponse<unknown> } }).response
    if (response?.data?.message) return response.data.message
    const errors = response?.data?.errors
    if (errors) {
      const first = Object.values(errors)[0]
      if (first?.[0]) return first[0]
    }
  }
  if (error instanceof Error) return error.message
  return fallback
}

export function formatPrice(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

export function resolveMediaUrl(url?: string | null) {
  if (!url) return null
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  return url
}

export async function unwrap<T>(promise: Promise<{ data: ApiResponse<T> }>) {
  const { data } = await promise
  if (!data.success) {
    throw new Error(data.message ?? 'Request failed')
  }
  return data.data
}
