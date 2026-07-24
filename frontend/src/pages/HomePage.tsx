import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { productsApi } from '../features/products/api'
import { categoriesApi } from '../features/categories/api'
import { ProductCard } from '../components/common/ProductCard'
import { Button, EmptyState, Input, Select, Spinner } from '../components/common/ui'
import type { ProductCondition } from '../types'

export function HomePage() {
  const [keyword, setKeyword] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [condition, setCondition] = useState<ProductCondition | ''>('')
  const [location, setLocation] = useState('')
  const [page, setPage] = useState(1)

  const filters = useMemo(
    () => ({
      page,
      pageSize: 12,
      keyword: keyword || undefined,
      categoryId: categoryId || undefined,
      condition: condition || undefined,
      location: location || undefined,
      sortBy: 'createdAt',
      sortDirection: 'desc' as const,
    }),
    [page, keyword, categoryId, condition, location],
  )

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: () => categoriesApi.list(),
  })

  const productsQuery = useQuery({
    queryKey: ['products', filters],
    queryFn: () => productsApi.list(filters),
  })

  return (
    <div>
      <section className="relative overflow-hidden border-b border-line">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(31,95,74,0.18),transparent_35%),radial-gradient(circle_at_80%_0%,rgba(243,201,107,0.25),transparent_30%)]" />
        <div className="relative mx-auto grid max-w-6xl gap-8 px-4 py-16 md:grid-cols-[1.1fr_0.9fr] md:px-6 md:py-24">
          <div className="space-y-5">
            <p className="text-sm font-semibold uppercase tracking-[0.2em] text-forest">Pass đồ thông minh</p>
            <h1 className="font-display text-4xl leading-tight text-ink md:text-6xl">
              PassDo
            </h1>
            <p className="max-w-xl text-base text-muted md:text-lg">
              Đăng bán lại đồ cá nhân còn dùng được — nhanh, rõ ràng, gần bạn.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link to="/products/new">
                <Button>Đăng bán ngay</Button>
              </Link>
              <a href="#listing">
                <Button variant="secondary">Xem đồ đang pass</Button>
              </a>
            </div>
          </div>
          <div className="min-h-56 rounded-[2rem] bg-[linear-gradient(145deg,#1f5f4a_0%,#2f7d62_45%,#f3c96b_100%)] p-8 text-white shadow-lg">
            <p className="font-display text-3xl">Đồ đẹp không nên bỏ phí</p>
            <p className="mt-4 max-w-sm text-sm text-white/85">
              Mỹ phẩm, thời trang, điện tử, đồ gia dụng — tìm người cần và pass tiếp vòng đời sản phẩm.
            </p>
          </div>
        </div>
      </section>

      <section id="listing" className="mx-auto max-w-6xl px-4 py-10 md:px-6">
        <div className="mb-6 grid gap-3 rounded-2xl border border-line bg-white/80 p-4 md:grid-cols-4">
          <Input
            placeholder="Tìm theo tên..."
            value={keyword}
            onChange={(e) => {
              setPage(1)
              setKeyword(e.target.value)
            }}
          />
          <Select
            value={categoryId}
            onChange={(e) => {
              setPage(1)
              setCategoryId(e.target.value)
            }}
          >
            <option value="">Tất cả danh mục</option>
            {categoriesQuery.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
          <Select
            value={condition}
            onChange={(e) => {
              setPage(1)
              setCondition(e.target.value as ProductCondition | '')
            }}
          >
            <option value="">Mọi tình trạng</option>
            <option value="New">New</option>
            <option value="LikeNew">LikeNew</option>
            <option value="Used">Used</option>
            <option value="Damaged">Damaged</option>
          </Select>
          <Input
            placeholder="Khu vực..."
            value={location}
            onChange={(e) => {
              setPage(1)
              setLocation(e.target.value)
            }}
          />
        </div>

        {productsQuery.isLoading && <Spinner />}
        {productsQuery.isError && (
          <EmptyState title="Không tải được sản phẩm" description="Thử lại sau vài giây." />
        )}
        {productsQuery.data && productsQuery.data.items.length === 0 && (
          <EmptyState title="Chưa có sản phẩm phù hợp" description="Thử đổi bộ lọc hoặc đăng bán món đầu tiên." />
        )}
        {productsQuery.data && productsQuery.data.items.length > 0 && (
          <>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {productsQuery.data.items.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
            <div className="mt-8 flex items-center justify-center gap-3">
              <Button
                variant="secondary"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Trước
              </Button>
              <span className="text-sm text-muted">
                Trang {productsQuery.data.page}/{productsQuery.data.totalPages || 1}
              </span>
              <Button
                variant="secondary"
                disabled={page >= (productsQuery.data.totalPages || 1)}
                onClick={() => setPage((p) => p + 1)}
              >
                Sau
              </Button>
            </div>
          </>
        )}
      </section>
    </div>
  )
}
