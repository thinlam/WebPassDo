import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { favoritesApi } from '../features/favorites/api'
import { Button, EmptyState, PageHeader, Section, Spinner } from '../components/common/ui'
import { formatPrice, getErrorMessage, resolveMediaUrl } from '../utils/api'
import { useState } from 'react'

export function FavoritesPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState('')

  const query = useQuery({
    queryKey: ['favorites'],
    queryFn: () => favoritesApi.list(),
  })

  const removeMutation = useMutation({
    mutationFn: (productId: string) => favoritesApi.remove(productId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['favorites'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  return (
    <Section>
      <PageHeader title="Yêu thích" description="Những món bạn đã lưu để xem lại." />
      {error && <p className="mb-4 text-sm text-rose-700">{error}</p>}
      {query.isLoading && <Spinner />}
      {query.data?.items.length === 0 && <EmptyState title="Chưa có sản phẩm yêu thích" />}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {query.data?.items.map((item) => {
          const image = resolveMediaUrl(item.primaryImageUrl)
          return (
            <div key={item.id} className="overflow-hidden rounded-2xl border border-line bg-white/80">
              <Link to={`/products/${item.productId}`}>
                <div className="aspect-[4/3] bg-sand">
                  {image ? <img src={image} alt="" className="h-full w-full object-cover" /> : null}
                </div>
                <div className="space-y-1 p-4">
                  <h3 className="font-medium">{item.productName}</h3>
                  <p className="text-forest">{formatPrice(item.sellingPrice)}</p>
                  <p className="text-xs text-muted">{item.location}</p>
                </div>
              </Link>
              <div className="px-4 pb-4">
                <Button
                  variant="ghost"
                  className="w-full"
                  onClick={() => removeMutation.mutate(item.productId)}
                >
                  Bỏ yêu thích
                </Button>
              </div>
            </div>
          )
        })}
      </div>
    </Section>
  )
}
