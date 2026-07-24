import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { useState } from 'react'
import { ordersApi } from '../features/orders/api'
import { Badge, Button, EmptyState, PageHeader, Section, Spinner } from '../components/common/ui'
import { getErrorMessage, resolveMediaUrl } from '../utils/api'
import {
  formatVND,
  getStatusLabel,
  getStatusTone,
  getDeliverySpeedLabel,
  formatDateRange,
} from '../lib/orderStatus'

type Tab = 'available' | 'mine'

export function ShipperOrdersPage() {
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab) ?? 'available'
  const queryClient = useQueryClient()
  const [error, setError] = useState('')

  const availableQuery = useQuery({
    queryKey: ['shipper-orders', 'available'],
    queryFn: () => ordersApi.shipperOrders({ page: 1, pageSize: 50, availableOnly: true }),
    enabled: tab === 'available',
  })

  const mineQuery = useQuery({
    queryKey: ['shipper-orders', 'mine'],
    queryFn: () => ordersApi.shipperOrders({ page: 1, pageSize: 50, availableOnly: false }),
    enabled: tab === 'mine',
  })

  const claimM = useMutation({
    mutationFn: (id: string) => ordersApi.claim(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipper-orders'] })
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const pickupM = useMutation({
    mutationFn: (id: string) => ordersApi.confirmPickup(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipper-orders'] })
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const deliveredM = useMutation({
    mutationFn: (id: string) => ordersApi.confirmDelivered(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipper-orders'] })
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const failM = useMutation({
    mutationFn: (id: string) => ordersApi.failDelivery(id, 'Giao không thành công'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipper-orders'] })
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const data = tab === 'available' ? availableQuery.data : mineQuery.data
  const isLoading = tab === 'available' ? availableQuery.isLoading : mineQuery.isLoading

  return (
    <Section>
      <PageHeader title="Đơn shipper" description="Quản lý đơn giao hàng." />
      <div className="mb-4 flex gap-2 border-b border-line">
        <button
          className={`px-4 py-2 text-sm font-medium transition ${tab === 'available' ? 'border-b-2 border-forest text-forest' : 'text-muted hover:text-ink'}`}
          onClick={() => setParams({ tab: 'available' })}
        >
          Đơn chờ nhận
        </button>
        <button
          className={`px-4 py-2 text-sm font-medium transition ${tab === 'mine' ? 'border-b-2 border-forest text-forest' : 'text-muted hover:text-ink'}`}
          onClick={() => setParams({ tab: 'mine' })}
        >
          Đơn của tôi
        </button>
      </div>

      {error && <p className="mb-4 text-sm text-rose-700">{error}</p>}
      {isLoading && <Spinner />}
      {data?.items.length === 0 && <EmptyState title="Không có đơn nào" />}

      <div className="space-y-3">
        {data?.items.map((order) => {
          const image = resolveMediaUrl(order.productImageUrl)
          return (
            <div
              key={order.id}
              className="flex flex-col gap-4 rounded-2xl border border-line bg-white/80 p-4 sm:flex-row sm:items-center"
            >
              <div className="h-20 w-28 overflow-hidden rounded-xl bg-sand">
                {image ? <img src={image} alt="" className="h-full w-full object-cover" /> : null}
              </div>
              <div className="flex-1 space-y-1">
                <Link to={`/orders/${order.id}`} className="font-medium hover:text-forest">
                  {order.productName}
                </Link>
                <p className="text-sm text-forest">{formatVND(order.grandTotal)}</p>
                <p className="text-xs text-muted">
                  {getDeliverySpeedLabel(order.deliverySpeed)} · {order.quantity}x
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
              <div className="flex flex-wrap gap-2">
                {tab === 'available' && order.status === 'AwaitingPickup' && !order.shipperId && (
                  <Button onClick={() => claimM.mutate(order.id)} disabled={claimM.isPending}>
                    Nhận đơn
                  </Button>
                )}
                {tab === 'mine' && order.status === 'AwaitingPickup' && (
                  <Button onClick={() => pickupM.mutate(order.id)} disabled={pickupM.isPending}>
                    Đã lấy hàng
                  </Button>
                )}
                {tab === 'mine' && order.status === 'Shipping' && (
                  <>
                    <Button onClick={() => deliveredM.mutate(order.id)} disabled={deliveredM.isPending}>
                      Đã giao
                    </Button>
                    <Button variant="danger" onClick={() => failM.mutate(order.id)} disabled={failM.isPending}>
                      Giao thất bại
                    </Button>
                  </>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </Section>
  )
}
