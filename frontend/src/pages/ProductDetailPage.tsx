import { useMutation, useQuery } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useState } from 'react'
import { productsApi } from '../features/products/api'
import { favoritesApi } from '../features/favorites/api'
import { useAuthStore } from '../stores/authStore'
import { Badge, Button, EmptyState, Spinner } from '../components/common/ui'
import { formatPrice, getErrorMessage, resolveMediaUrl } from '../utils/api'

export function ProductDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const productQuery = useQuery({
    queryKey: ['product', id],
    queryFn: () => productsApi.getById(id),
    enabled: !!id,
  })

  const favoriteMutation = useMutation({
    mutationFn: () => favoritesApi.add(id),
    onSuccess: () => {
      setMessage('Đã thêm vào yêu thích')
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (productQuery.isLoading) return <Spinner />
  if (productQuery.isError || !productQuery.data) {
    return <EmptyState title="Không tìm thấy sản phẩm" />
  }

  const product = productQuery.data
  const images = product.images?.length ? product.images : []
  const primary = resolveMediaUrl(images.find((i) => i.isPrimary)?.url ?? images[0]?.url)
  const isOwner = user?.id === product.sellerId

  return (
    <div className="mx-auto grid max-w-6xl gap-8 px-4 py-8 md:grid-cols-2 md:px-6">
      <div className="overflow-hidden rounded-3xl border border-line bg-white">
        <div className="aspect-[4/3] bg-sand">
          {primary ? (
            <img src={primary} alt={product.name} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full items-center justify-center text-muted">Chưa có ảnh</div>
          )}
        </div>
        {images.length > 1 && (
          <div className="grid grid-cols-4 gap-2 p-3">
            {images.map((img) => {
              const url = resolveMediaUrl(img.url)
              return url ? (
                <img
                  key={img.id}
                  src={url}
                  alt=""
                  className="aspect-square rounded-lg object-cover"
                />
              ) : null
            })}
          </div>
        )}
      </div>

      <div className="space-y-5">
        <div className="flex flex-wrap gap-2">
          <Badge tone="success">{product.status}</Badge>
          <Badge>{product.condition}</Badge>
          <Badge tone="neutral">{product.categoryName ?? 'Danh mục'}</Badge>
        </div>
        <h1 className="font-display text-4xl text-ink">{product.name}</h1>
        <p className="font-display text-3xl text-forest">{formatPrice(product.sellingPrice)}</p>
        <p className="text-sm text-muted">
          Giá gốc {formatPrice(product.originalPrice)} · {product.location}
        </p>
        {product.quantity > 1 && (
          <p className="text-sm text-muted">Còn {product.quantity} sản phẩm</p>
        )}
        <p className="whitespace-pre-wrap text-muted">{product.description}</p>
        <p className="text-sm text-muted">Người bán: {product.sellerName ?? '—'}</p>

        {message && <p className="text-sm text-emerald-800">{message}</p>}
        {error && <p className="text-sm text-rose-700">{error}</p>}

        <div className="flex flex-wrap gap-3">
          {isAuthenticated && !isOwner && product.status === 'Available' && (
            <>
              <Button onClick={() => navigate(`/checkout/${id}`)}>Mua</Button>
              <Button
                variant="secondary"
                onClick={() => favoriteMutation.mutate()}
                disabled={favoriteMutation.isPending}
              >
                Yêu thích
              </Button>
            </>
          )}
          {isOwner && (
            <>
              <Link to={`/products/${id}/edit`}>
                <Button variant="secondary">Chỉnh sửa</Button>
              </Link>
              <Link to="/my-products">
                <Button variant="ghost">Quản lý sản phẩm</Button>
              </Link>
            </>
          )}
          {!isAuthenticated && (
            <Button onClick={() => navigate('/login')}>Đăng nhập để mua</Button>
          )}
        </div>
      </div>
    </div>
  )
}
