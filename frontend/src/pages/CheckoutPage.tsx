import { useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { productsApi } from '../features/products/api'
import { ordersApi } from '../features/orders/api'
import { addressesApi } from '../features/addresses/api'
import { shippingApi } from '../features/shipping/api'
import { Button, EmptyState, Section, Select, Spinner } from '../components/common/ui'
import { getErrorMessage, resolveMediaUrl } from '../utils/api'
import {
  formatVND,
  getDeliverySpeedLabel,
  formatDateRange,
} from '../lib/orderStatus'
import type { DeliverySpeed, OrderDetail, PaymentMethod } from '../types'

export function CheckoutPage() {
  const { productId = '' } = useParams()
  const navigate = useNavigate()
  const [addressId, setAddressId] = useState('')
  const [speed, setSpeed] = useState<DeliverySpeed>('Standard')
  const [payment, setPayment] = useState<PaymentMethod>('CashOnDelivery')
  const [quantity, setQuantity] = useState(1)
  const [note, setNote] = useState('')
  const [error, setError] = useState('')
  const [createdOrder, setCreatedOrder] = useState<OrderDetail | null>(null)

  const productQuery = useQuery({
    queryKey: ['product', productId],
    queryFn: () => productsApi.getById(productId),
    enabled: !!productId,
  })

  const addressesQuery = useQuery({
    queryKey: ['my-addresses'],
    queryFn: () => addressesApi.list(),
  })

  useEffect(() => {
    if (addressesQuery.data) {
      const def = addressesQuery.data.find((a) => a.isDefault)
      if (def) setAddressId(def.id)
    }
  }, [addressesQuery.data])

  useEffect(() => {
    const p = productQuery.data
    if (p) {
      const speeds = p.allowedDeliverySpeeds ?? []
      if (speeds.length > 0 && !speeds.includes(speed)) {
        setSpeed(speeds[0] as DeliverySpeed)
      }
      const opt = p.acceptedPaymentOption
      if (opt === 'BankTransfer') setPayment('BankTransfer')
      else if (opt === 'CashOnDelivery') setPayment('CashOnDelivery')
    }
  }, [productQuery.data])

  const shippingQuery = useQuery({
    queryKey: ['shipping-calculate', productId, addressId, speed],
    queryFn: () => shippingApi.calculate({ productId, shippingAddressId: addressId, deliverySpeed: speed }),
    enabled: !!productId && !!addressId,
  })

  const previewQuery = useQuery({
    queryKey: ['order-preview', productId, quantity, addressId, speed, payment],
    queryFn: () =>
      ordersApi.preview({
        productId,
        quantity,
        shippingAddressId: addressId || null,
        deliverySpeed: speed,
        paymentMethod: payment,
      }),
    enabled: !!productId && !!addressId,
  })

  const createMutation = useMutation({
    mutationFn: () =>
      ordersApi.create({
        productId,
        quantity,
        shippingAddressId: addressId,
        deliverySpeed: speed,
        paymentMethod: payment,
        note: note || undefined,
      }),
    onSuccess: (order) => {
      setCreatedOrder(order)
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (productQuery.isLoading) return <Spinner />
  if (!productQuery.data) return <EmptyState title="Không tìm thấy sản phẩm" />

  const product = productQuery.data
  const preview = previewQuery.data
  const shippingCalc = shippingQuery.data
  const image = resolveMediaUrl(product.images?.find((i) => i.isPrimary)?.url ?? product.images?.[0]?.url)
  const allowedSpeeds = product.allowedDeliverySpeeds ?? []
  const payOpt = product.acceptedPaymentOption

  if (createdOrder) {
    return (
      <Section className="max-w-xl">
        <div className="space-y-4 rounded-2xl border border-line bg-white/80 p-6 text-center">
          <h2 className="font-display text-2xl text-forest">Đặt hàng thành công!</h2>
          <p className="text-sm text-muted">Mã đơn: {createdOrder.orderCode}</p>
          {createdOrder.paymentMethod === 'BankTransfer' && createdOrder.sellerBankAccount && (
            <div className="mx-auto max-w-sm rounded-xl border border-line bg-sand/50 p-4 text-left text-sm">
              <p className="mb-2 font-medium text-ink">Thông tin chuyển khoản</p>
              <p>Ngân hàng: {createdOrder.sellerBankAccount.bankName}</p>
              <p>STK: {createdOrder.sellerBankAccount.accountNumber}</p>
              <p>Chủ TK: {createdOrder.sellerBankAccount.accountHolderName}</p>
              {createdOrder.sellerBankAccount.branch && (
                <p>Chi nhánh: {createdOrder.sellerBankAccount.branch}</p>
              )}
              {createdOrder.payment?.transferContent && (
                <p className="mt-2 font-medium text-forest">
                  Nội dung CK: {createdOrder.payment.transferContent}
                </p>
              )}
            </div>
          )}
          <div className="flex justify-center gap-3">
            <Button onClick={() => navigate(`/orders/${createdOrder.id}`)}>Xem đơn hàng</Button>
            <Button variant="secondary" onClick={() => navigate('/purchases')}>
              Đơn mua
            </Button>
          </div>
        </div>
      </Section>
    )
  }

  return (
    <Section className="max-w-2xl">
      <h1 className="mb-6 font-display text-3xl text-ink">Thanh toán</h1>
      <div className="grid gap-6 md:grid-cols-5">
        <div className="space-y-4 md:col-span-3">
          <div className="flex gap-4 rounded-2xl border border-line bg-white/80 p-4">
            <div className="h-20 w-24 overflow-hidden rounded-xl bg-sand">
              {image && <img src={image} alt="" className="h-full w-full object-cover" />}
            </div>
            <div className="flex-1">
              <p className="font-medium text-ink">{product.name}</p>
              <p className="text-sm text-forest">{formatVND(product.sellingPrice)}</p>
              <p className="text-xs text-muted">{product.location}</p>
            </div>
          </div>

          <div className="space-y-3 rounded-2xl border border-line bg-white/80 p-4">
            <Select
              label="Địa chỉ giao hàng"
              value={addressId}
              onChange={(e) => setAddressId(e.target.value)}
            >
              <option value="">Chọn địa chỉ</option>
              {addressesQuery.data?.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.recipientName} – {a.fullAddress}
                  {a.isDefault ? ' (mặc định)' : ''}
                </option>
              ))}
            </Select>
            {addressesQuery.data?.length === 0 && (
              <p className="text-xs text-muted">
                Chưa có địa chỉ.{' '}
                <a href="/settings?tab=addresses" className="text-forest hover:underline">
                  Thêm địa chỉ →
                </a>
              </p>
            )}

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="block space-y-1.5">
                <span className="text-sm font-medium text-ink">Số lượng</span>
                <input
                  type="number"
                  min={1}
                  max={product.quantity}
                  value={quantity}
                  onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                  className="w-full rounded-md border border-line bg-white px-3 py-2 text-sm outline-none ring-forest/30 focus:ring-2"
                />
              </label>
              <Select
                label="Tốc độ giao"
                value={speed}
                onChange={(e) => setSpeed(e.target.value as DeliverySpeed)}
              >
                {allowedSpeeds.map((s) => (
                  <option key={s} value={s}>
                    {getDeliverySpeedLabel(s)}
                  </option>
                ))}
              </Select>
            </div>

            <Select
              label="Phương thức thanh toán"
              value={payment}
              onChange={(e) => setPayment(e.target.value as PaymentMethod)}
            >
              {(payOpt === 'Both' || payOpt === 'CashOnDelivery') && (
                <option value="CashOnDelivery">Thanh toán khi nhận hàng</option>
              )}
              {(payOpt === 'Both' || payOpt === 'BankTransfer') && (
                <option value="BankTransfer">Chuyển khoản</option>
              )}
            </Select>

            <label className="block space-y-1.5">
              <span className="text-sm font-medium text-ink">Ghi chú</span>
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={2}
                className="w-full rounded-md border border-line bg-white px-3 py-2 text-sm outline-none ring-forest/30 focus:ring-2"
              />
            </label>
          </div>
        </div>

        <div className="md:col-span-2">
          <div className="sticky top-24 space-y-3 rounded-2xl border border-line bg-white/80 p-4">
            <h3 className="font-display text-lg text-ink">Tổng cộng</h3>
            {previewQuery.isLoading && <Spinner />}
            {preview && (
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted">Tiền hàng</span>
                  <span>{formatVND(preview.productTotal)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted">Phí vận chuyển</span>
                  <span>{shippingCalc ? formatVND(shippingCalc.fee) : formatVND(preview.shippingFee)}</span>
                </div>
                {shippingCalc?.fee === 0 && (
                  <p className="text-xs text-emerald-700">Miễn phí giao hàng nội thành</p>
                )}
                <div className="border-t border-line pt-2">
                  <div className="flex justify-between font-medium">
                    <span>Tổng thanh toán</span>
                    <span className="text-forest">{formatVND(preview.grandTotal)}</span>
                  </div>
                </div>
                {preview.etaNote && (
                  <p className="text-xs text-muted">Dự kiến: {preview.etaNote}</p>
                )}
                {(preview.estimatedDeliveryFromPreview || preview.estimatedDeliveryToPreview) && (
                  <p className="text-xs text-muted">
                    ETA:{' '}
                    {formatDateRange(
                      preview.estimatedDeliveryFromPreview,
                      preview.estimatedDeliveryToPreview,
                    )}
                  </p>
                )}
              </div>
            )}
            {!addressId && (
              <p className="text-xs text-rose-700">Vui lòng chọn địa chỉ giao hàng</p>
            )}
            {error && <p className="text-sm text-rose-700">{error}</p>}
            <Button
              className="w-full"
              disabled={!addressId || createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              {createMutation.isPending ? 'Đang đặt...' : 'Đặt hàng'}
            </Button>
          </div>
        </div>
      </div>
    </Section>
  )
}
