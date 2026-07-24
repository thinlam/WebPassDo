import { Link } from 'react-router-dom'
import type { ProductListItem } from '../../types'
import { formatPrice, resolveMediaUrl } from '../../utils/api'
import { Badge } from '../common/ui'

export function ProductCard({ product }: { product: ProductListItem }) {
  const image = resolveMediaUrl(product.primaryImageUrl)

  return (
    <Link
      to={`/products/${product.id}`}
      className="group block overflow-hidden rounded-2xl border border-line bg-white/80 transition hover:-translate-y-0.5 hover:shadow-md"
    >
      <div className="aspect-[4/3] overflow-hidden bg-sand">
        {image ? (
          <img
            src={image}
            alt={product.name}
            className="h-full w-full object-cover transition duration-500 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-muted">Chưa có ảnh</div>
        )}
      </div>
      <div className="space-y-2 p-4">
        <div className="flex items-start justify-between gap-2">
          <h3 className="line-clamp-2 font-medium text-ink">{product.name}</h3>
          <Badge>{product.condition}</Badge>
        </div>
        <p className="font-display text-xl text-forest">{formatPrice(product.sellingPrice)}</p>
        <p className="text-xs text-muted">{product.location}</p>
      </div>
    </Link>
  )
}
