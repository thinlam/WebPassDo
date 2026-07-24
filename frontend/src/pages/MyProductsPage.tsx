import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { productsApi } from '../features/products/api'
import { Badge, Button, EmptyState, PageHeader, Section, Spinner } from '../components/common/ui'
import { formatPrice, getErrorMessage, resolveMediaUrl } from '../utils/api'
import { useState } from 'react'
import type { ProductStatus } from '../types'

export function MyProductsPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState('')

  const query = useQuery({
    queryKey: ['my-products'],
    queryFn: () => productsApi.myProducts({ page: 1, pageSize: 50 }),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ProductStatus }) =>
      productsApi.updateStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-products'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => productsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-products'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  return (
    <Section>
      <PageHeader
        title="Đồ của tôi"
        description="Quản lý sản phẩm bạn đang đăng bán."
        actions={
          <Link to="/products/new">
            <Button>Đăng bán mới</Button>
          </Link>
        }
      />
      {error && <p className="mb-4 text-sm text-rose-700">{error}</p>}
      {query.isLoading && <Spinner />}
      {query.data?.items.length === 0 && <EmptyState title="Bạn chưa đăng sản phẩm nào" />}
      <div className="space-y-3">
        {query.data?.items.map((item) => {
          const image = resolveMediaUrl(item.primaryImageUrl)
          return (
            <div
              key={item.id}
              className="flex flex-col gap-4 rounded-2xl border border-line bg-white/80 p-4 sm:flex-row sm:items-center"
            >
              <div className="h-20 w-28 overflow-hidden rounded-xl bg-sand">
                {image ? <img src={image} alt="" className="h-full w-full object-cover" /> : null}
              </div>
              <div className="flex-1 space-y-1">
                <Link to={`/products/${item.id}`} className="font-medium hover:text-forest">
                  {item.name}
                </Link>
                <p className="text-sm text-forest">{formatPrice(item.sellingPrice)}</p>
                <div className="flex gap-2">
                  <Badge>{item.status}</Badge>
                  <Badge tone="neutral">{item.condition}</Badge>
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                <Link to={`/products/${item.id}/edit`}>
                  <Button variant="secondary">Sửa</Button>
                </Link>
                {item.status === 'Draft' || item.status === 'Hidden' ? (
                  <Button
                    variant="secondary"
                    onClick={() => statusMutation.mutate({ id: item.id, status: 'Available' })}
                  >
                    Mở bán
                  </Button>
                ) : null}
                {item.status === 'Available' ? (
                  <Button
                    variant="secondary"
                    onClick={() => statusMutation.mutate({ id: item.id, status: 'Hidden' })}
                  >
                    Ẩn
                  </Button>
                ) : null}
                <Button variant="danger" onClick={() => deleteMutation.mutate(item.id)}>
                  Xóa
                </Button>
              </div>
            </div>
          )
        })}
      </div>
    </Section>
  )
}
