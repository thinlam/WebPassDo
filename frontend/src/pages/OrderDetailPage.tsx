import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { useState } from 'react'
import { ordersApi } from '../features/orders/api'
import { useAuthStore } from '../stores/authStore'
import { Badge, Button, EmptyState, Input, Section, Spinner } from '../components/common/ui'
import { getErrorMessage, resolveMediaUrl } from '../utils/api'
import {
  formatVND,
  formatDate,
  formatDateRange,
  getStatusLabel,
  getStatusTone,
  getDeliverySpeedLabel,
  getPaymentMethodLabel,
  getPaymentStatusLabel,
} from '../lib/orderStatus'
import type { HandOverPayload } from '../types'

export function OrderDetailPage() {
  const { id = '' } = useParams()
  const queryClient = useQueryClient()
  const user = useAuthStore((s) => s.user)
  const [error, setError] = useState('')
  const [actionNote, setActionNote] = useState('')
  const [showHandoverModal, setShowHandoverModal] = useState(false)

  const query = useQuery({
    queryKey: ['order', id],
    queryFn: () => ordersApi.getById(id),
    enabled: !!id,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['order', id] })
    queryClient.invalidateQueries({ queryKey: ['purchases'] })
    queryClient.invalidateQueries({ queryKey: ['sales'] })
    setError('')
    setActionNote('')
  }

  const act = (fn: () => Promise<unknown>) => ({
    mutationFn: fn,
    onSuccess: invalidate,
    onError: (err: unknown) => setError(getErrorMessage(err)),
  })

  const confirmPaymentM = useMutation(act(() => ordersApi.confirmPayment(id, actionNote || undefined)))
  const confirmM = useMutation(act(() => ordersApi.confirm(id, actionNote || undefined)))
  const rejectM = useMutation(act(() => ordersApi.reject(id, actionNote || 'Từ chối')))
  const cancelM = useMutation(act(() => ordersApi.cancel(id, actionNote || undefined)))
  const markPreparedM = useMutation(act(() => ordersApi.markPrepared(id)))
  const confirmDeliveredM = useMutation(act(() => ordersApi.confirmDelivered(id)))
  const handOverM = useMutation(act(() => ordersApi.handOver(id, handoverForm)))
  const failDeliveryM = useMutation(act(() => ordersApi.failDelivery(id, actionNote || 'Giao thất bại')))

  const [proofUrl, setProofUrl] = useState('')
  const proofM = useMutation(act(() => ordersApi.paymentProof(id, proofUrl)))

  const [handoverForm, setHandoverForm] = useState<HandOverPayload>({
    deliveryPersonName: '',
    phone: '',
    company: '',
    vehicleNumber: '',
    trackingCode: '',
    note: '',
    estimatedDeliveryTime: '',
  })

  if (query.isLoading) return <Spinner />
  if (!query.data) return <EmptyState title="Không tìm thấy đơn hàng" />

  const o = query.data
  const isBuyer = user?.id === o.buyerId
  const isSeller = user?.id === o.sellerId || user?.role === 'Admin'
  const image = resolveMediaUrl(o.productImageUrl)

  const courierInfo = o.shipment

  return (
    <Section className="max-w-3xl">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl text-ink">Đơn hàng #{o.orderCode}</h1>
          <p className="text-sm text-muted">Tạo lúc {formatDate(o.createdAt)}</p>
        </div>
        <Badge tone={getStatusTone(o.status)}>{getStatusLabel(o.status)}</Badge>
      </div>

      <div className="space-y-4">
        {/* Product */}
        <Card title="Sản phẩm">
          <div className="flex gap-4">
            <div className="h-16 w-20 overflow-hidden rounded-lg bg-sand">
              {image && <img src={image} alt="" className="h-full w-full object-cover" />}
            </div>
            <div className="flex-1 space-y-1">
              <Link to={`/products/${o.productId}`} className="font-medium hover:text-forest">
                {o.productName}
              </Link>
              <p className="text-sm text-muted">
                {o.quantity}x · {formatVND(o.productTotal)}
              </p>
            </div>
          </div>
          {o.items?.map((item) => (
            <p key={item.productId} className="mt-2 text-xs text-muted">
              Đơn giá: {formatVND(item.unitPrice)} × {item.quantity} = {formatVND(item.lineTotal)}
            </p>
          ))}
        </Card>

        {/* Parties */}
        <div className="grid gap-4 sm:grid-cols-2">
          {o.seller && (
            <Card title="Người bán">
              <p className="font-medium">{o.seller.fullName}</p>
              {o.seller.phoneNumber && <p className="text-sm text-muted">{o.seller.phoneNumber}</p>}
            </Card>
          )}
          {o.buyer && (
            <Card title="Người mua">
              <p className="font-medium">{o.buyer.fullName}</p>
              {o.buyer.phoneNumber && <p className="text-sm text-muted">{o.buyer.phoneNumber}</p>}
            </Card>
          )}
        </div>

        {/* Shipping */}
        <Card title="Vận chuyển">
          <div className="space-y-1 text-sm">
            <p>Tốc độ: {getDeliverySpeedLabel(o.deliverySpeed)}</p>
            <p>Phí ship: {formatVND(o.shippingFee)}</p>
            {(o.estimatedDeliveryFrom || o.estimatedDeliveryTo) && (
              <p>ETA: {formatDateRange(o.estimatedDeliveryFrom, o.estimatedDeliveryTo)}</p>
            )}
            {o.shippingAddress && (
              <p className="text-muted">Giao đến: {o.shippingAddress.fullAddress}</p>
            )}
            {o.pickupAddress && (
              <p className="text-muted">Lấy hàng: {o.pickupAddress.fullAddress}</p>
            )}
            {o.shipment?.trackingCode && <p>Mã vận đơn: {o.shipment.trackingCode}</p>}
          </div>
        </Card>

        {/* Courier info for buyer when Shipping */}
        {isBuyer && o.status === 'Shipping' && courierInfo && (courierInfo.shipperName || courierInfo.shipperPhone) && (
          <Card title="Thông tin vận chuyển">
            <div className="space-y-2 text-sm">
              {courierInfo.shipperName && <p>Người giao: {courierInfo.shipperName}</p>}
              {courierInfo.shipperPhone && (
                <div className="flex gap-3">
                  <span>SĐT: {courierInfo.shipperPhone}</span>
                  <a href={`tel:${courierInfo.shipperPhone}`} className="text-forest hover:underline">Gọi</a>
                  <a href={`sms:${courierInfo.shipperPhone}`} className="text-forest hover:underline">SMS</a>
                </div>
              )}
              {courierInfo.carrierName && <p>Đơn vị: {courierInfo.carrierName}</p>}
              {courierInfo.trackingCode && <p>Mã vận đơn: {courierInfo.trackingCode}</p>}
              {courierInfo.deliveryNote && <p>Ghi chú: {courierInfo.deliveryNote}</p>}
            </div>
          </Card>
        )}

        {/* Payment */}
        <Card title="Thanh toán">
          <div className="space-y-1 text-sm">
            <p>Phương thức: {getPaymentMethodLabel(o.paymentMethod)}</p>
            <p>Trạng thái: {getPaymentStatusLabel(o.paymentStatus)}</p>
            <p className="font-medium text-forest">Tổng: {formatVND(o.grandTotal)}</p>
            {o.payment?.transferContent && (
              <p className="text-muted">Nội dung CK: {o.payment.transferContent}</p>
            )}
            {o.payment?.proofImageUrl && (
              <div className="mt-2">
                <p className="text-xs text-muted">Ảnh minh chứng:</p>
                <img
                  src={resolveMediaUrl(o.payment.proofImageUrl) ?? ''}
                  alt="proof"
                  className="mt-1 max-w-xs rounded-lg"
                />
              </div>
            )}
          </div>
          {o.sellerBankAccount && o.paymentMethod === 'BankTransfer' && (
            <div className="mt-3 rounded-lg border border-line bg-sand/30 p-3 text-sm">
              <p className="mb-1 font-medium">Thông tin chuyển khoản</p>
              <p>Ngân hàng: {o.sellerBankAccount.bankName}</p>
              <p>STK: {o.sellerBankAccount.accountNumber}</p>
              <p>Chủ TK: {o.sellerBankAccount.accountHolderName}</p>
              {o.sellerBankAccount.branch && <p>Chi nhánh: {o.sellerBankAccount.branch}</p>}
            </div>
          )}
        </Card>

        {/* Timeline */}
        {o.statusHistory?.length > 0 && (
          <Card title="Lịch sử trạng thái">
            <div className="space-y-2">
              {o.statusHistory.map((h, i) => (
                <div key={i} className="flex gap-3 text-sm">
                  <span className="w-32 shrink-0 text-muted">{formatDate(h.createdAt)}</span>
                  <div>
                    <Badge tone={getStatusTone(h.newStatus)}>{getStatusLabel(h.newStatus)}</Badge>
                    {h.changedByName && (
                      <span className="ml-2 text-xs text-muted">bởi {h.changedByName}</span>
                    )}
                    {h.note && <p className="mt-0.5 text-xs text-muted">{h.note}</p>}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {o.note && (
          <Card title="Ghi chú">
            <p className="whitespace-pre-wrap text-sm text-muted">{o.note}</p>
          </Card>
        )}
        {o.cancellationReason && (
          <Card title="Lý do hủy">
            <p className="text-sm text-rose-700">{o.cancellationReason}</p>
          </Card>
        )}

        {/* Actions */}
        {error && <p className="text-sm text-rose-700">{error}</p>}

        <div className="space-y-3 rounded-2xl border border-line bg-white/80 p-4">
          <Input
            label="Ghi chú hành động"
            value={actionNote}
            onChange={(e) => setActionNote(e.target.value)}
            placeholder="Lý do / ghi chú (nếu cần)"
          />
          <div className="flex flex-wrap gap-2">
            {/* Buyer actions */}
            {isBuyer && o.status === 'AwaitingPayment' && o.paymentMethod === 'BankTransfer' && (
              <div className="flex w-full gap-2">
                <Input
                  label="URL ảnh minh chứng"
                  value={proofUrl}
                  onChange={(e) => setProofUrl(e.target.value)}
                  className="flex-1"
                />
                <Button
                  onClick={() => proofM.mutate()}
                  disabled={!proofUrl || proofM.isPending}
                  className="mt-auto"
                >
                  Gửi minh chứng
                </Button>
              </div>
            )}
            {isBuyer && o.status === 'Delivered' && (
              <Button onClick={() => confirmDeliveredM.mutate()} disabled={confirmDeliveredM.isPending}>
                Xác nhận đã nhận
              </Button>
            )}
            {isBuyer && ['AwaitingPayment', 'PendingConfirmation'].includes(o.status) && (
              <Button variant="danger" onClick={() => cancelM.mutate()} disabled={cancelM.isPending}>
                Hủy đơn
              </Button>
            )}

            {/* Seller actions */}
            {isSeller && o.status === 'AwaitingPayment' && o.paymentStatus === 'AwaitingConfirmation' && (
              <Button onClick={() => confirmPaymentM.mutate()} disabled={confirmPaymentM.isPending}>
                Xác nhận thanh toán
              </Button>
            )}
            {isSeller && o.status === 'PendingConfirmation' && (
              <>
                <Button onClick={() => confirmM.mutate()} disabled={confirmM.isPending}>
                  Xác nhận đơn
                </Button>
                <Button variant="danger" onClick={() => rejectM.mutate()} disabled={rejectM.isPending}>
                  Từ chối
                </Button>
              </>
            )}
            {isSeller && (o.status === 'AwaitingPreparation' || (o.status === 'AwaitingPickup' && !o.preparedAt)) && (
              <Button onClick={() => markPreparedM.mutate()} disabled={markPreparedM.isPending}>
                Đã chuẩn bị hàng
              </Button>
            )}
            {isSeller && (o.status === 'AwaitingHandover' || (o.status === 'AwaitingPickup' && !!o.preparedAt)) && (
              <Button onClick={() => setShowHandoverModal(true)}>
                Bàn giao cho vận chuyển
              </Button>
            )}
            {isSeller && ['AwaitingPayment', 'PendingConfirmation'].includes(o.status) && (
              <Button variant="danger" onClick={() => rejectM.mutate()} disabled={rejectM.isPending}>
                Từ chối / Hủy đơn
              </Button>
            )}
            {isSeller && o.status === 'Shipping' && (
              <Button variant="danger" onClick={() => failDeliveryM.mutate()} disabled={failDeliveryM.isPending}>
                Giao thất bại
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Handover Modal */}
      {showHandoverModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md space-y-4 rounded-2xl bg-white p-6 shadow-xl">
            <h2 className="font-display text-xl text-ink">Bàn giao đơn hàng</h2>
            <Input
              label="Tên người giao *"
              value={handoverForm.deliveryPersonName}
              onChange={(e) => setHandoverForm((f) => ({ ...f, deliveryPersonName: e.target.value }))}
              required
            />
            <Input
              label="Số điện thoại *"
              value={handoverForm.phone}
              onChange={(e) => setHandoverForm((f) => ({ ...f, phone: e.target.value }))}
              required
            />
            <Input
              label="Đơn vị vận chuyển *"
              value={handoverForm.company}
              onChange={(e) => setHandoverForm((f) => ({ ...f, company: e.target.value }))}
              required
            />
            <Input
              label="Biển số xe"
              value={handoverForm.vehicleNumber}
              onChange={(e) => setHandoverForm((f) => ({ ...f, vehicleNumber: e.target.value }))}
            />
            <Input
              label="Mã vận đơn"
              value={handoverForm.trackingCode}
              onChange={(e) => setHandoverForm((f) => ({ ...f, trackingCode: e.target.value }))}
            />
            <Input
              label="Ghi chú"
              value={handoverForm.note}
              onChange={(e) => setHandoverForm((f) => ({ ...f, note: e.target.value }))}
            />
            <Input
              label="Thời gian dự kiến giao"
              type="datetime-local"
              value={handoverForm.estimatedDeliveryTime}
              onChange={(e) => setHandoverForm((f) => ({ ...f, estimatedDeliveryTime: e.target.value }))}
            />
            <div className="flex gap-3">
              <Button
                onClick={() => handOverM.mutate()}
                disabled={!handoverForm.deliveryPersonName || !handoverForm.phone || !handoverForm.company || handOverM.isPending}
              >
                {handOverM.isPending ? 'Đang gửi...' : 'Xác nhận bàn giao'}
              </Button>
              <Button variant="ghost" onClick={() => setShowHandoverModal(false)}>
                Hủy
              </Button>
            </div>
          </div>
        </div>
      )}
    </Section>
  )
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-line bg-white/80 p-4">
      <h3 className="mb-3 font-display text-lg text-ink">{title}</h3>
      {children}
    </div>
  )
}
