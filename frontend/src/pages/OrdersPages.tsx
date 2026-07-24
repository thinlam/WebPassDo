import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { ordersApi } from '../features/orders/api'
import { Badge, EmptyState, PageHeader, Section, Spinner } from '../components/common/ui'
import { resolveMediaUrl } from '../utils/api'
import {
  formatVND,
  getStatusLabel,
  getStatusTone,
  getDeliverySpeedLabel,
  formatDateRange,
} from '../lib/orderStatus'
import type { OrderStatus } from '../types'

const STATUS_TABS: { key: string; label: string; value?: OrderStatus }[] = [
  { key: 'all', label: 'Tất cả' },
  { key: 'AwaitingPayment', label: 'Chờ thanh toán', value: 'AwaitingPayment' },
  { key: 'PendingConfirmation', label: 'Chờ xác nhận', value: 'PendingConfirmation' },
  { key: 'AwaitingPreparation', label: 'Chờ chuẩn bị hàng', value: 'AwaitingPreparation' },
  { key: 'AwaitingHandover', label: 'Chờ bàn giao', value: 'AwaitingHandover' },
  { key: 'Shipping', label: 'Đang giao', value: 'Shipping' },
  { key: 'Delivered', label: 'Đã giao', value: 'Delivered' },
  { key: 'Cancelled', label: 'Đã hủy', value: 'Cancelled' },
]

function StatusTabs({
  current,
  onChange,
}: {
  current: string
  onChange: (key: string) => void
}) {
  return (
    <div className="mb-4 flex gap-1 overflow-x-auto border-b border-line">
      {STATUS_TABS.map((t) => (
        <button
          key={t.key}
          className={`whitespace-nowrap px-3 py-2 text-sm font-medium transition ${current === t.key ? 'border-b-2 border-forest text-forest' : 'text-muted hover:text-ink'}`}
          onClick={() => onChange(t.key)}
        >
          {t.label}
        </button>
      ))}
    </div>
  )
}

export function PurchasesPage() {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? 'all'
  const statusFilter = STATUS_TABS.find((t) => t.key === tab)?.value

  const query = useQuery({
    queryKey: ['purchases', tab],
    queryFn: () => ordersApi.myPurchases({ page: 1, pageSize: 50, status: statusFilter }),
  })

  return (
    <Section>
      <PageHeader title="Đơn mua" description="Các đơn hàng bạn đã đặt." />
      <StatusTabs
        current={tab}
        onChange={(k) => setParams(k === 'all' ? {} : { tab: k })}
      />
      {query.isLoading && <Spinner />}
      {query.data?.items.length === 0 && (
        <EmptyState title="Không có đơn hàng" description="Chưa có đơn nào trong trạng thái này." />
      )}
      <div className="space-y-3">
        {query.data?.items.map((order) => {
          const image = resolveMediaUrl(order.productImageUrl)
          return (
            <Link
              key={order.id}
              to={`/orders/${order.id}`}
              className="flex flex-col gap-4 rounded-2xl border border-line bg-white/80 p-4 transition hover:-translate-y-0.5 hover:shadow-md sm:flex-row sm:items-center"
            >
              <div className="h-20 w-28 overflow-hidden rounded-xl bg-sand">
                {image ? <img src={image} alt="" className="h-full w-full object-cover" /> : null}
              </div>
              <div className="flex-1 space-y-1">
                <p className="font-medium text-ink">{order.productName}</p>
                <p className="text-sm text-forest">{formatVND(order.grandTotal)}</p>
                <p className="text-xs text-muted">
                  {order.quantity}x · {getDeliverySpeedLabel(order.deliverySpeed)}
                </p>
                {(order.estimatedDeliveryFrom || order.estimatedDeliveryTo) && (
                  <p className="text-xs text-muted">
                    ETA: {formatDateRange(order.estimatedDeliveryFrom, order.estimatedDeliveryTo)}
                  </p>
                )}
                <div className="flex gap-2">
                  <Badge tone={getStatusTone(order.status)}>{getStatusLabel(order.status)}</Badge>
                </div>
              </div>
            </Link>
          )
        })}
      </div>
    </Section>
  )
}

export function SalesPage() {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? 'all'
  const statusFilter = STATUS_TABS.find((t) => t.key === tab)?.value

  const query = useQuery({
    queryKey: ['sales', tab],
    queryFn: () => ordersApi.mySales({ page: 1, pageSize: 50, status: statusFilter }),
  })

  return (
    <Section>
      <PageHeader title="Đơn bán" description="Đơn hàng khách đã đặt cho sản phẩm của bạn." />
      <StatusTabs
        current={tab}
        onChange={(k) => setParams(k === 'all' ? {} : { tab: k })}
      />
      {query.isLoading && <Spinner />}
      {query.data?.items.length === 0 && (
        <EmptyState title="Không có đơn hàng" description="Chưa có đơn nào trong trạng thái này." />
      )}
      <div className="space-y-3">
        {query.data?.items.map((order) => {
          const image = resolveMediaUrl(order.productImageUrl)
          return (
            <Link
              key={order.id}
              to={`/orders/${order.id}`}
              className="flex flex-col gap-4 rounded-2xl border border-line bg-white/80 p-4 transition hover:-translate-y-0.5 hover:shadow-md sm:flex-row sm:items-center"
            >
              <div className="h-20 w-28 overflow-hidden rounded-xl bg-sand">
                {image ? <img src={image} alt="" className="h-full w-full object-cover" /> : null}
              </div>
              <div className="flex-1 space-y-1">
                <p className="font-medium text-ink">{order.productName}</p>
                <p className="text-sm text-forest">{formatVND(order.grandTotal)}</p>
                <p className="text-xs text-muted">
                  Người mua: {order.buyerName} · {order.quantity}x
                </p>
                <div className="flex gap-2">
                  <Badge tone={getStatusTone(order.status)}>{getStatusLabel(order.status)}</Badge>
                </div>
              </div>
            </Link>
          )
        })}
      </div>
    </Section>
  )
}
