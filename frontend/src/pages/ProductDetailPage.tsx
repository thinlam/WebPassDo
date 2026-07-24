import { useMutation, useQuery } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { productsApi } from '../features/products/api'
import { favoritesApi } from '../features/favorites/api'
import { chatApi } from '../features/chat/api'
import { useAuthStore } from '../stores/authStore'
import { Badge, Button, EmptyState, Spinner } from '../components/common/ui'
import { formatPrice, getErrorMessage, resolveMediaUrl } from '../utils/api'
import { PresenceLabel } from '../components/presence/PresenceLabel'
import { usePresenceHub } from '../features/presence/usePresenceHub'

export function ProductDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const { subscribePresence } = usePresenceHub()
  const [sellerPresence, setSellerPresence] = useState<{ isOnline?: boolean; lastSeenAt?: string | null }>({})

  const productQuery = useQuery({
    queryKey: ['product', id],
    queryFn: () => productsApi.getById(id),
    enabled: !!id,
  })

  const sellerId = productQuery.data?.sellerId

  useEffect(() => {
    setSellerPresence({})
  }, [sellerId])

  useEffect(() => {
    if (!isAuthenticated || !sellerId) return
    const unsubscribe = subscribePresence((evt) => {
      if (evt.userId !== sellerId) return
      setSellerPresence({ isOnline: evt.isOnline, lastSeenAt: evt.lastSeenAt })
    })
    return () => unsubscribe()
  }, [isAuthenticated, sellerId, subscribePresence])

  const favoriteMutation = useMutation({
    mutationFn: () => favoritesApi.add(id),
    onSuccess: () => {
      setMessage('Đã thêm vào yêu thích')
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const chatMutation = useMutation({
    mutationFn: (productId: string) => chatApi.getOrCreate(productId),
    onSuccess: (conv) => {
      setError('')
      navigate(`/messages/${conv.id}`)
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
  const sellerIsOnline = sellerPresence.isOnline ?? product.sellerIsOnline
  const sellerLastSeenAt = sellerPresence.lastSeenAt ?? product.sellerLastSeenAt

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

        {/* Seller info */}
        {!isOwner && (
          <div className="rounded-2xl border border-line bg-sand/30 p-4">
            <h3 className="mb-2 font-medium text-ink">Người bán</h3>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-forest/10 text-sm font-bold text-forest">
                {product.sellerName?.charAt(0)?.toUpperCase() || '?'}
              </div>
              <div>
                <p className="font-medium text-ink">{product.sellerName ?? '—'}</p>
                <PresenceLabel isOnline={sellerIsOnline} lastSeenAt={sellerLastSeenAt} />
              </div>
            </div>
            {isAuthenticated && (
              <div className="mt-3 flex flex-wrap gap-2">
                <Button
                  variant="secondary"
                  onClick={() => {
                    const productId = product.id || id
                    if (!productId) {
                      setError('Thiếu mã sản phẩm. Hãy tải lại trang.')
                      return
                    }
                    chatMutation.mutate(productId)
                  }}
                  disabled={chatMutation.isPending}
                >
                  {chatMutation.isPending ? 'Đang mở...' : 'Nhắn tin'}
                </Button>
              </div>
            )}
          </div>
        )}

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
