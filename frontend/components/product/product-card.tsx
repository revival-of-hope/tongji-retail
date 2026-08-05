import Link from "next/link"
import { PackageOpen, Star } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { currency } from "@/lib/format"
import type { ProductListItem } from "@/lib/api/generated/types.gen"

export function ProductCard({ product }: { product: ProductListItem }) {
  return (
    <Card className="group overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
      <Link href={`/products/${product.id}`} aria-label={`查看${product.name}`}>
        <div className="aspect-[4/3] overflow-hidden bg-muted">
          {product.mainImageUrl ? <img src={product.mainImageUrl} alt={product.name} className="h-full w-full object-cover transition duration-300 group-hover:scale-105" /> : <div className="grid h-full place-items-center"><PackageOpen className="size-10 text-muted-foreground" /></div>}
        </div>
        <CardContent className="space-y-3 p-4">
          <div className="flex items-start justify-between gap-2"><h2 className="line-clamp-2 font-medium">{product.name}</h2><Badge variant="secondary">{product.categoryName}</Badge></div>
          <div className="flex items-end justify-between gap-3"><span className="text-xl font-bold text-primary">{currency(product.price)}</span><span className="text-xs text-muted-foreground">已售 {product.soldCount}</span></div>
          <div className="flex justify-between text-xs text-muted-foreground"><span>{product.storeName}</span><span className="flex items-center gap-1"><Star className="size-3 fill-current" />{product.avgRating.toFixed(1)} ({product.reviewCount})</span></div>
        </CardContent>
      </Link>
    </Card>
  )
}
