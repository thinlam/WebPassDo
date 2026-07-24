import type { OrderStatus } from '../types'

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  AwaitingPayment: 'Chờ thanh toán',
  PendingConfirmation: 'Chờ xác nhận',
  AwaitingPickup: 'Chờ lấy hàng',
  Shipping: 'Đang giao hàng',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
  DeliveryFailed: 'Giao thất bại',
  Returned: 'Trả hàng',
  Refunded: 'Hoàn tiền',
}

export function getStatusLabel(status: string): string {
  return ORDER_STATUS_LABELS[status as OrderStatus] ?? status
}

export function getStatusTone(status: string) {
  switch (status) {
    case 'Delivered':
      return 'success' as const
    case 'Cancelled':
    case 'DeliveryFailed':
    case 'Returned':
    case 'Refunded':
      return 'danger' as const
    case 'Shipping':
    case 'AwaitingPickup':
      return 'warn' as const
    default:
      return 'neutral' as const
  }
}

export const DELIVERY_SPEED_LABELS: Record<string, string> = {
  Express: 'Hỏa tốc',
  SameDay: 'Trong ngày',
  Standard: 'Tiêu chuẩn',
  Intercity: 'Liên tỉnh',
}

export function getDeliverySpeedLabel(speed: string): string {
  return DELIVERY_SPEED_LABELS[speed] ?? speed
}

export const PAYMENT_METHOD_LABELS: Record<string, string> = {
  BankTransfer: 'Chuyển khoản',
  CashOnDelivery: 'Thanh toán khi nhận hàng',
}

export function getPaymentMethodLabel(method: string): string {
  return PAYMENT_METHOD_LABELS[method] ?? method
}

export const PAYMENT_STATUS_LABELS: Record<string, string> = {
  Unpaid: 'Chưa thanh toán',
  AwaitingConfirmation: 'Chờ xác nhận',
  Paid: 'Đã thanh toán',
  Refunded: 'Đã hoàn tiền',
}

export function getPaymentStatusLabel(status: string): string {
  return PAYMENT_STATUS_LABELS[status] ?? status
}

export function formatVND(value: number): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

export function formatDate(date: string | null | undefined): string {
  if (!date) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
}

export function formatDateRange(from?: string | null, to?: string | null): string {
  if (!from && !to) return '—'
  const fmt = (d: string) =>
    new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit' }).format(new Date(d))
  if (from && to) return `${fmt(from)} – ${fmt(to)}`
  if (from) return `Từ ${fmt(from)}`
  return `Đến ${fmt(to!)}`
}
