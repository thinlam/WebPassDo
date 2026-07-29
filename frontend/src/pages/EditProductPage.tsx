import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { productsApi } from '../features/products/api'
import { categoriesApi } from '../features/categories/api'
import { addressesApi } from '../features/addresses/api'
import { bankAccountsApi } from '../features/bankAccounts/api'
import {
  Button,
  EmptyState,
  Input,
  PageHeader,
  Section,
  Select,
  Spinner,
  TextArea,
} from '../components/common/ui'
import { getErrorMessage, resolveMediaUrl } from '../utils/api'
import type { AcceptedPaymentOption, DeliverySpeed } from '../types'

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
  pickupAddressId: z.string().optional(),
  bankAccountId: z.string().optional(),
  acceptedPaymentOption: z.enum(['BankTransfer', 'CashOnDelivery', 'Both']),
})

type FormValues = z.infer<typeof schema>

export function EditProductPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState('')
  const [newFiles, setNewFiles] = useState<FileList | null>(null)
  const [speeds, setSpeeds] = useState<DeliverySpeed[]>(['Standard', 'Intercity'])
  const [speedsInitialized, setSpeedsInitialized] = useState(false)

  const productQuery = useQuery({
    queryKey: ['product', id],
    queryFn: () => productsApi.getById(id),
    enabled: !!id,
  })

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

  const product = productQuery.data

  useEffect(() => {
    setSpeedsInitialized(false)
  }, [id])

  useEffect(() => {
    if (!product || speedsInitialized) return
    const next = (product.allowedDeliverySpeeds ?? []).filter((s): s is DeliverySpeed =>
      ALL_SPEEDS.includes(s as DeliverySpeed),
    )
    setSpeeds(next.length > 0 ? next : ['Standard', 'Intercity'])
    setSpeedsInitialized(true)
  }, [product, speedsInitialized])

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: product
      ? {
          name: product.name ?? '',
          description: product.description ?? '',
          originalPrice: Number(product.originalPrice) || 0,
          sellingPrice: Number(product.sellingPrice) || 0,
          condition: (['New', 'LikeNew', 'Used', 'Damaged'].includes(String(product.condition))
            ? product.condition
            : 'Used') as FormValues['condition'],
          categoryId: String(product.categoryId ?? ''),
          location: product.location ?? '',
          quantity: Math.max(1, Number(product.quantity) || 1),
          pickupAddressId: product.pickupAddressId ? String(product.pickupAddressId) : '',
          bankAccountId: product.bankAccountId ? String(product.bankAccountId) : '',
          acceptedPaymentOption: (['BankTransfer', 'CashOnDelivery', 'Both'].includes(
            String(product.acceptedPaymentOption),
          )
            ? product.acceptedPaymentOption
            : 'Both') as AcceptedPaymentOption,
        }
      : undefined,
  })

  const toggleSpeed = (s: DeliverySpeed) =>
    setSpeeds((prev) => (prev.includes(s) ? prev.filter((x) => x !== s) : [...prev, s]))

  const updateMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const updated = await productsApi.update(id, {
        ...values,
        pickupAddressId: values.pickupAddressId || null,
        bankAccountId: values.bankAccountId || null,
        allowedDeliverySpeeds: speeds.length > 0 ? speeds : ['Standard', 'Intercity'],
      })
      if (newFiles) {
        for (const file of Array.from(newFiles).slice(0, 5)) {
          await productsApi.uploadImage(updated.id, file)
        }
      }
      return updated
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['product', id] })
      queryClient.invalidateQueries({ queryKey: ['my-products'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      navigate('/my-products')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const deleteImageMutation = useMutation({
    mutationFn: (imageId: string) => productsApi.deleteImage(id, imageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['product', id] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  const setPrimaryMutation = useMutation({
    mutationFn: (imageId: string) => productsApi.setPrimaryImage(id, imageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['product', id] }),
  })

  if (!id) {
    return <EmptyState title="Thiếu mã sản phẩm" description="Quay lại Đồ của tôi và thử lại." />
  }

  if (productQuery.isLoading || categoriesQuery.isLoading) {
    return (
      <Section className="max-w-2xl">
        <PageHeader title="Chỉnh sửa sản phẩm" />
        <Spinner />
      </Section>
    )
  }

  if (productQuery.isError) {
    return (
      <Section className="max-w-2xl">
        <EmptyState
          title="Không tải được sản phẩm"
          description={getErrorMessage(productQuery.error)}
        />
        <Button className="mt-4" onClick={() => productQuery.refetch()}>
          Thử lại
        </Button>
      </Section>
    )
  }

  if (!product) {
    return <EmptyState title="Không tìm thấy sản phẩm" description="Sản phẩm có thể đã bị xóa." />
  }

  return (
    <Section className="max-w-2xl">
      <PageHeader title="Chỉnh sửa sản phẩm" description={product.name} />
      {product.hasActiveOrders && (
        <p className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Sản phẩm đang có đơn xử lý — không thể đổi giá.
        </p>
      )}
      <form
        className="space-y-4 rounded-2xl border border-line bg-white/80 p-6"
        onSubmit={handleSubmit((v) => updateMutation.mutate(v))}
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
            disabled={product.hasActiveOrders}
            error={errors.originalPrice?.message}
            {...register('originalPrice', { valueAsNumber: true })}
          />
          <Input
            label="Giá bán"
            type="number"
            disabled={product.hasActiveOrders}
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
            <option value="New">Mới</option>
            <option value="LikeNew">Như mới</option>
            <option value="Used">Đã dùng</option>
            <option value="Damaged">Hư hỏng nhẹ</option>
          </Select>
        </div>
        <Select label="Danh mục" error={errors.categoryId?.message} {...register('categoryId')}>
          <option value="">Chọn danh mục</option>
          {(categoriesQuery.data ?? []).map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </Select>
        <Input label="Khu vực (nhãn)" error={errors.location?.message} {...register('location')} />

        <Select label="Địa chỉ lấy hàng" {...register('pickupAddressId')}>
          <option value="">Chọn địa chỉ</option>
          {(addressesQuery.data ?? []).map((a) => (
            <option key={a.id} value={a.id}>
              {a.recipientName} – {a.fullAddress}
            </option>
          ))}
        </Select>

        <Select label="Tài khoản nhận tiền" {...register('bankAccountId')}>
          <option value="">Chọn tài khoản</option>
          {(banksQuery.data ?? []).map((b) => (
            <option key={b.id} value={b.id}>
              {b.bankName} – {b.accountNumberMasked}
            </option>
          ))}
        </Select>

        <Select
          label="Phương thức thanh toán chấp nhận"
          error={errors.acceptedPaymentOption?.message}
          {...register('acceptedPaymentOption')}
        >
          <option value="Both">Cả hai</option>
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

        {(product.images ?? []).length > 0 && (
          <div className="space-y-2">
            <span className="text-sm font-medium text-ink">Hình ảnh hiện tại</span>
            <div className="grid grid-cols-4 gap-2">
              {product.images.map((img) => {
                const url = resolveMediaUrl(img.url)
                return (
                  <div key={img.id} className="relative">
                    {url && (
                      <img
                        src={url}
                        alt=""
                        className={`aspect-square w-full rounded-lg object-cover ${img.isPrimary ? 'ring-2 ring-forest' : ''}`}
                      />
                    )}
                    <div className="absolute right-1 top-1 flex gap-1">
                      {!img.isPrimary && (
                        <button
                          type="button"
                          className="rounded bg-white/90 px-1 text-xs text-forest shadow"
                          onClick={() => setPrimaryMutation.mutate(img.id)}
                        >
                          ★
                        </button>
                      )}
                      <button
                        type="button"
                        className="rounded bg-white/90 px-1 text-xs text-rose-700 shadow"
                        onClick={() => deleteImageMutation.mutate(img.id)}
                      >
                        ✕
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        <label className="block space-y-1.5">
          <span className="text-sm font-medium">Thêm hình ảnh</span>
          <input
            type="file"
            accept="image/*"
            multiple
            onChange={(e) => setNewFiles(e.target.files)}
            className="block w-full text-sm"
          />
        </label>

        {error && <p className="text-sm text-rose-700">{error}</p>}
        <div className="flex gap-3">
          <Button type="submit" disabled={isSubmitting || updateMutation.isPending}>
            {updateMutation.isPending ? 'Đang lưu...' : 'Lưu thay đổi'}
          </Button>
          <Button type="button" variant="ghost" onClick={() => navigate('/my-products')}>
            Hủy
          </Button>
        </div>
      </form>
    </Section>
  )
}
