"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import { PackageOpen, ShoppingCart, Star } from "lucide-react"
import { toast } from "sonner"
import { Navbar } from "@/components/layout/navbar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Skeleton } from "@/components/ui/skeleton"
import { api } from "@/lib/api/sdk"
import type { ProductDetail, ProductReviewResponse } from "@/lib/api/generated/types.gen"
import { currency, dateTime } from "@/lib/format"
import { useAuthStore } from "@/store/auth"

export default function ProductDetailPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const user = useAuthStore((state) => state.user)
  const [product, setProduct] = useState<ProductDetail | null>(null)
  const [reviews, setReviews] = useState<ProductReviewResponse[]>([])
  const [quantity, setQuantity] = useState(1)
  const [activeImage, setActiveImage] = useState(0)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const id = Number(params.id)
    setLoading(true)
    void Promise.all([api.product(id), api.productReviews(id)])
      .then(([detail, productReviews]) => {
        setProduct(detail)
        setReviews(productReviews)
      })
      .catch((error) => toast.error(error instanceof Error ? error.message : "商品加载失败"))
      .finally(() => setLoading(false))
  }, [params.id])

  const addToCart = async () => {
    if (!product) return
    if (!user) {
      router.push("/auth/login")
      return
    }
    if (user.role !== "Customer") {
      toast.error("只有顾客账号可以购买商品")
      return
    }
    try {
      await api.addCartItem({ productId: product.id, quantity })
      toast.success("已加入购物车")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "加入购物车失败")
    }
  }

  if (loading || !product) {
    return <><Navbar /><main className="mx-auto grid max-w-6xl gap-8 px-4 py-8 md:grid-cols-2"><Skeleton className="aspect-square" /><div className="space-y-4"><Skeleton className="h-6 w-1/3" /><Skeleton className="h-12 w-4/5" /><Skeleton className="h-28" /><Skeleton className="h-12 w-1/2" /></div></main></>
  }

  const image = product.imageUrls[activeImage]
  return (
    <div className="min-h-screen bg-muted/20">
      <Navbar />
      <main className="mx-auto max-w-6xl space-y-8 px-4 py-8">
        <Card>
          <CardContent className="grid gap-8 p-5 md:grid-cols-2">
            <div className="space-y-3">
              <div className="aspect-square overflow-hidden rounded-xl bg-muted">
                {image ? <img src={image} alt={product.name} className="h-full w-full object-cover" /> : <div className="grid h-full place-items-center"><PackageOpen className="size-16 text-muted-foreground" /></div>}
              </div>
              {product.imageUrls.length > 1 && <div className="flex gap-2 overflow-x-auto">{product.imageUrls.map((url, index) => <Button key={url} variant={activeImage === index ? "default" : "outline"} size="icon" className="size-16 shrink-0 overflow-hidden p-0" onClick={() => setActiveImage(index)}><img src={url} alt={`${product.name} 图片 ${index + 1}`} className="h-full w-full object-cover" /></Button>)}</div>}
            </div>
            <div className="flex flex-col">
              <div className="flex flex-wrap gap-2"><Badge variant="secondary">{product.categoryName}</Badge><Badge variant="outline">{product.storeName}</Badge></div>
              <h1 className="mt-4 text-3xl font-semibold">{product.name}</h1>
              <p className="mt-4 whitespace-pre-wrap text-muted-foreground">{product.description || "暂无商品描述"}</p>
              <p className="mt-6 text-3xl font-bold text-primary">{currency(product.price)}</p>
              <div className="mt-3 flex flex-wrap gap-4 text-sm text-muted-foreground"><span>库存 {product.stockQuantity}</span><span>已售 {product.soldCount}</span><span className="flex items-center gap-1"><Star className="size-4 fill-current" />{product.avgRating.toFixed(1)}（{product.reviewCount}）</span></div>
              <Separator className="my-6" />
              <div className="mt-auto flex items-center gap-3">
                <Input className="w-24" type="number" min={1} max={product.stockQuantity} value={quantity} onChange={(event) => setQuantity(Math.min(product.stockQuantity, Math.max(1, Number(event.target.value) || 1)))} />
                <Button className="flex-1" disabled={product.stockQuantity < 1} onClick={() => void addToCart()}><ShoppingCart />加入购物车</Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>商品评价</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            {reviews.length === 0 && <p className="text-sm text-muted-foreground">暂无评价</p>}
            {reviews.map((review) => <article key={review.id} className="rounded-lg border p-4"><div className="flex flex-wrap items-center justify-between gap-2 text-sm"><strong>{review.username}</strong><span className="text-amber-500">{"★".repeat(review.rating)}<span className="text-muted-foreground">{"★".repeat(5 - review.rating)}</span></span></div><p className="mt-2 text-sm">{review.comment || "用户未填写文字评价"}</p><p className="mt-2 text-xs text-muted-foreground">{dateTime(review.createdAt)}</p></article>)}
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
