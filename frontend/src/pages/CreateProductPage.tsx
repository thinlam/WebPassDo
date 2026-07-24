import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { productsApi } from '../features/products/api'
import { categoriesApi } from '../features/categories/api'
import { addressesApi } from '../features/addresses/api'
import { bankAccountsApi } from '../features/bankAccounts/api'
import { useAuthStore } from '../stores/authStore'
import { Button, Input, PageHeader, Section, Select, TextArea } from '../components/common/ui'
import { getErrorMessage } from '../utils/api'
import type { DeliverySpeed } from '../types'

const ALL_SPEEDS: DeliverySpeed[] = ['Express', 'SameDay', 'Standard', 'Intercity']
const SPEED_LABELS: Record<string, string> = {
  Express: 'Hỏa tốc',
  SameDay: 'Trong ngày',
  Standard: 'Tiêu chuẩn',
  Intercity: 'Liên tỉnh',
}

const schema = z.object({
  name: z.string().min(2, 'Nhập tên sản phẩm'),
  description: z.string().min(10, 'Mô tả tối thiểu 10 ký tự'),
  originalPrice: z.number().min(0),
  sellingPrice: z.number().positive('Giá bán phải > 0'),
  condition: z.enum(['New', 'LikeNew', 'Used', 'Damaged']),
  categoryId: z.string().min(1, 'Chọn danh mục'),
  location: z.string().min(2, 'Nhập khu vực'),
  quantity: z.number().int().min(1, 'Số lượng ≥ 1'),
  status: z.enum(['Draft', 'Available', 'Hidden']),
  pickupAddressId: z.string().min(1, 'Vui lòng chọn địa chỉ lấy hàng'),
  bankAccountId: z.string().optional(),
  acceptedPaymentOption: z.enum(['BankTransfer', 'CashOnDelivery', 'Both']),
})

type FormValues = z.infer<typeof schema>

export function CreateProductPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = useAuthStore((s) => s.user)
  const [files, setFiles] = useState<FileList | null>(null)
  const [error, setError] = useState('')
  const [speeds, setSpeeds] = useState<DeliverySpeed[]>(['Standard', 'Intercity'])

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoriesApi.list(),
  })

  const addressesQuery = useQuery({
    queryKey: ['my-addresses'],
    queryFn: () => addressesApi.list(),
  })

  const banksQuery = useQuery({
    queryKey: ['my-bank-accounts'],
    queryFn: () => bankAccountsApi.list(),
  })

  const defaultAddress = addressesQuery.data?.find((a) => a.isDefault)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      condition: 'Used',
      status: 'Available',
      originalPrice: 0,
      sellingPrice: 0,
      quantity: 1,
      acceptedPaymentOption: 'Both',
      pickupAddressId: defaultAddress?.id ?? '',
    },
  })

  const paymentOption = watch('acceptedPaymentOption')
  const bankAccountId = watch('bankAccountId')

  const toggleSpeed = (s: DeliverySpeed) =>
    setSpeeds((prev) => (prev.includes(s) ? prev.filter((x) => x !== s) : [...prev, s]))

  const missingPhone = !user?.phoneNumber
  const missingAddresses = addressesQuery.data?.length === 0
  const needsBank = (paymentOption === 'BankTransfer' || paymentOption === 'Both') && !bankAccountId && banksQuery.data?.length === 0

  const createMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      if (!files || files.length === 0) {
        throw new Error('Vui lòng chọn ít nhất 1 hình ảnh')
      }
      const product = await productsApi.create({
        ...values,
        pickupAddressId: values.pickupAddressId || null,
        bankAccountId: values.bankAccountId || null,
        allowedDeliverySpeeds: speeds.length > 0 ? speeds : ['Standard', 'Intercity'],
      })
      const list = Array.from(files).slice(0, 5)
      for (let i = 0; i < list.length; i++) {
        await productsApi.uploadImage(product.id, list[i], i === 0)
      }
      return product
    },
    onSuccess: (product) => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['my-products'] })
      navigate(`/products/${product.id}`)
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const canSubmit = !missingPhone && !missingAddresses && !needsBank

  return (
    <Section className="max-w-2xl">
      <PageHeader title="Đăng bán" description="Đăng món đồ bạn muốn pass lại." />

      {/* Seller info */}
      <div className="mb-6 rounded-2xl border border-line bg-sand/30 p-4">
        <h3 className="mb-2 font-display text-sm font-medium text-ink">Thông tin người bán</h3>
        <p className="text-sm text-ink">{user?.fullName}</p>
        <p className="text-sm text-muted">{user?.phoneNumber || <span className="text-rose-700">Chưa có SĐT</span>}</p>
      </div>

      {/* Blocking warnings */}
      {(missingPhone || missingAddresses || needsBank) && (
        <div className="mb-6 space-y-2 rounded-2xl border border-rose-200 bg-rose-50 p-4">
          {missingPhone && (
            <p className="text-sm text-rose-700">
              Bạn chưa cập nhật số điện thoại.{' '}
              <Link to="/profile" className="font-medium underline">Cập nhật ngay →</Link>
            </p>
          )}
          {missingAddresses && (
            <p className="text-sm text-rose-700">
              Bạn chưa có địa chỉ lấy hàng.{' '}
              <Link to="/settings?tab=addresses" className="font-medium underline">Thêm địa chỉ →</Link>
            </p>
          )}
          {needsBank && (
            <p className="text-sm text-rose-700">
              Thanh toán chuyển khoản yêu cầu tài khoản ngân hàng.{' '}
              <Link to="/settings?tab=banks" className="font-medium underline">Thêm tài khoản →</Link>
            </p>
          )}
        </div>
      )}

      <form
        className="space-y-4 rounded-2xl border border-line bg-white/80 p-6"
        onSubmit={handleSubmit((values) => createMutation.mutate(values))}
      >
        <Input label="Tên sản phẩm" error={errors.name?.message} {...register('name')} />
        <TextArea
          label="Mô tả"
          rows={5}
          error={errors.description?.message}
          {...register('description')}
        />
        <div className="grid gap-4 sm:grid-cols-2">
          <Input
            label="Giá gốc"
            type="number"
            error={errors.originalPrice?.message}
            {...register('originalPrice', { valueAsNumber: true })}
          />
          <Input
            label="Giá bán"
            type="number"
            error={errors.sellingPrice?.message}
            {...register('sellingPrice', { valueAsNumber: true })}
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-3">
          <Input
            label="Số lượng"
            type="number"
            error={errors.quantity?.message}
            {...register('quantity', { valueAsNumber: true })}
          />
          <Select label="Tình trạng" error={errors.condition?.message} {...register('condition')}>
            <option value="New">New</option>
            <option value="LikeNew">LikeNew</option>
            <option value="Used">Used</option>
            <option value="Damaged">Damaged</option>
          </Select>
          <Select label="Trạng thái" error={errors.status?.message} {...register('status')}>
            <option value="Available">Available</option>
            <option value="Draft">Draft</option>
            <option value="Hidden">Hidden</option>
          </Select>
        </div>
        <Select label="Danh mục" error={errors.categoryId?.message} {...register('categoryId')}>
          <option value="">Chọn danh mục</option>
          {categoriesQuery.data?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </Select>
        <Input label="Khu vực" error={errors.location?.message} {...register('location')} />

        <div className="space-y-1">
          <Select label="Địa chỉ lấy hàng *" error={errors.pickupAddressId?.message} {...register('pickupAddressId')}>
            <option value="">Chọn địa chỉ</option>
            {addressesQuery.data?.map((a) => (
              <option key={a.id} value={a.id}>
                {a.recipientName} – {a.fullAddress}
                {a.isDefault ? ' (mặc định)' : ''}
              </option>
            ))}
          </Select>
          <Link to="/settings?tab=addresses" className="text-xs text-forest hover:underline">
            Quản lý địa chỉ →
          </Link>
        </div>

        <div className="space-y-1">
          <Select label="Tài khoản nhận tiền" {...register('bankAccountId')}>
            <option value="">Không chọn</option>
            {banksQuery.data?.map((b) => (
              <option key={b.id} value={b.id}>
                {b.bankName} – {b.accountNumberMasked}
              </option>
            ))}
          </Select>
          <Link to="/settings?tab=banks" className="text-xs text-forest hover:underline">
            Quản lý tài khoản ngân hàng →
          </Link>
        </div>

        <Select
          label="Phương thức thanh toán chấp nhận"
          error={errors.acceptedPaymentOption?.message}
          {...register('acceptedPaymentOption')}
        >
          <option value="Both">Tất cả</option>
          <option value="BankTransfer">Chuyển khoản</option>
          <option value="CashOnDelivery">COD</option>
        </Select>

        <fieldset className="space-y-1.5">
          <legend className="text-sm font-medium text-ink">Tốc độ giao hàng</legend>
          <div className="flex flex-wrap gap-3">
            {ALL_SPEEDS.map((s) => (
              <label key={s} className="flex items-center gap-1.5 text-sm">
                <input
                  type="checkbox"
                  checked={speeds.includes(s)}
                  onChange={() => toggleSpeed(s)}
                  className="accent-forest"
                />
                {SPEED_LABELS[s]}
              </label>
            ))}
          </div>
        </fieldset>

        <label className="block space-y-1.5">
          <span className="text-sm font-medium">Hình ảnh (tối đa 5, bắt buộc ít nhất 1) *</span>
          <input
            type="file"
            accept="image/*"
            multiple
            onChange={(e) => setFiles(e.target.files)}
            className="block w-full text-sm"
          />
          {!files?.length && <span className="text-xs text-muted">Chọn ít nhất 1 ảnh để đăng sản phẩm</span>}
        </label>
        {error && <p className="text-sm text-rose-700">{error}</p>}
        <Button type="submit" disabled={!canSubmit || isSubmitting || createMutation.isPending}>
          {createMutation.isPending ? 'Đang đăng...' : 'Đăng sản phẩm'}
        </Button>
      </form>
    </Section>
  )
}
