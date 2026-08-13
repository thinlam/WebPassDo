import type { OrderRejectReason, OrderStatus } from '../types'

export const ORDER_REJECT_REASON_LABELS: Record<OrderRejectReason, string> = {
  OutOfStock: 'Hết hàng',
  SoldElsewhere: 'Đã bán nơi khác',
  CannotDeliver: 'Không giao được',
  WrongPrice: 'Sai giá',
  Other: 'Khác',
}

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  AwaitingPayment: 'Chờ thanh toán',
  PendingSellerConfirmation: 'Chờ xác nhận',
  Preparing: 'Đang chuẩn bị hàng',
  ReadyForShipment: 'Chờ bàn giao',
  Shipping: 'Đang giao hàng',
  Delivered: 'Đã giao',
  Completed: 'Hoàn tất',
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
    case 'Completed':
      return 'success' as const
    case 'Cancelled':
    case 'DeliveryFailed':
    case 'Returned':
    case 'Refunded':
      return 'danger' as const
    case 'Shipping':
      return 'warn' as const
    case 'ReadyForShipment':
    case 'Preparing':
    case 'PendingSellerConfirmation':
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

import { parseApiDate } from './datetime'

const VN_TZ = 'Asia/Ho_Chi_Minh'

export function formatDate(date: string | null | undefined): string {
  if (!date) return '—'
  const d = parseApiDate(date)
  if (Number.isNaN(d.getTime())) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: VN_TZ,
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(d)
}

export function formatDateRange(from?: string | null, to?: string | null): string {
  if (!from && !to) return '—'
  const fmt = (d: string) => {
    const parsed = parseApiDate(d)
    if (Number.isNaN(parsed.getTime())) return '—'
    return new Intl.DateTimeFormat('vi-VN', {
      timeZone: VN_TZ,
      day: '2-digit',
      month: '2-digit',
    }).format(parsed)
  }
  if (from && to) return `${fmt(from)} – ${fmt(to)}`
  if (from) return `Từ ${fmt(from)}`
  return `Đến ${fmt(to!)}`
}
