import type { ProductStatus } from '../../types'

export const PRODUCT_STATUS_LABELS: Record<ProductStatus, string> = {
  Draft: 'Bản nháp',
  Active: 'Đang bán',
  Reserved: 'Đã được giữ',
  Sold: 'Đã bán',
  Hidden: 'Đã ẩn',
  Rejected: 'Bị từ chối',
  PendingReview: 'Chờ duyệt',
}

export function formatProductStatus(status: string): string {
  return PRODUCT_STATUS_LABELS[status as ProductStatus] ?? status
}

export function sellerStatusActions(
  status: ProductStatus,
): { label: string; next: ProductStatus }[] {
  switch (status) {
    case 'Draft':
      return [{ label: 'Gửi duyệt', next: 'PendingReview' }]
    case 'PendingReview':
      return [{ label: 'Rút duyệt', next: 'Draft' }]
    case 'Rejected':
      return [{ label: 'Về bản nháp', next: 'Draft' }]
    case 'Active':
      return [{ label: 'Ẩn', next: 'Hidden' }]
    case 'Hidden':
      return [{ label: 'Hiện lại', next: 'Active' }]
    default:
      return []
  }
}

export function canBuyStatus(status: string): boolean {
  return status === 'Active'
}

