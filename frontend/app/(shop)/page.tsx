"use client"

import { useCallback, useEffect, useState } from "react"
import { Search } from "lucide-react"
import { toast } from "sonner"
import { Navbar } from "@/components/layout/navbar"
import { ProductCard } from "@/components/product/product-card"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import { api } from "@/lib/api/sdk"
import type { CategoryResponse, GetProductsData, ProductListItem } from "@/lib/api/generated/types.gen"

type SortBy = NonNullable<NonNullable<GetProductsData["query"]>["sortBy"]>

const sortOptions: { value: SortBy; label: string }[] = [
  { value: "newest", label: "最新发布" },
  { value: "sales", label: "销量优先" },
  { value: "rating", label: "评分优先" },
  { value: "price_asc", label: "价格从低到高" },
  { value: "price_desc", label: "价格从高到低" },
]

export default function ShopPage() {
  const [products, setProducts] = useState<ProductListItem[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [keyword, setKeyword] = useState("")
  const [submittedKeyword, setSubmittedKeyword] = useState("")
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [sortBy, setSortBy] = useState<SortBy>("newest")
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.products({
        pageIndex: 1,
        pageSize: 60,
        keyword: submittedKeyword || undefined,
        categoryId,
        sortBy,
      })
      setProducts(result.items)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "商品加载失败")
    } finally {
      setLoading(false)
    }
  }, [categoryId, sortBy, submittedKeyword])

  useEffect(() => {
    void api.categories().then(setCategories).catch(() => toast.error("分类加载失败"))
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div className="min-h-screen bg-muted/20">
      <Navbar />
      <section className="border-b bg-background">
        <div className="mx-auto max-w-7xl px-4 py-12">
          <p className="text-sm font-medium text-primary">商品零售管理系统</p>
          <h1 className="mt-2 max-w-3xl text-3xl font-semibold tracking-tight md:text-5xl">
            从商品浏览到多角色运营，使用同一份 OpenAPI 契约
          </h1>
          <form
            className="mt-8 flex max-w-2xl gap-2"
            onSubmit={(event) => {
              event.preventDefault()
              setSubmittedKeyword(keyword.trim())
            }}
          >
            <div className="relative flex-1">
              <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="搜索商品名称或描述"
                className="pl-9"
              />
            </div>
            <Button type="submit">搜索</Button>
          </form>
        </div>
      </section>

      <main className="mx-auto max-w-7xl px-4 py-8">
        <Card className="mb-6">
          <CardContent className="flex flex-col gap-4 p-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap gap-2">
              <Button variant={categoryId === undefined ? "default" : "outline"} onClick={() => setCategoryId(undefined)}>
                全部
              </Button>
              {categories.map((category) => (
                <Button
                  key={category.id}
                  variant={categoryId === category.id ? "default" : "outline"}
                  onClick={() => setCategoryId(category.id)}
                >
                  {category.name}
                </Button>
              ))}
            </div>
            <Select value={sortBy} onValueChange={(value) => setSortBy(value as SortBy)}>
              <SelectTrigger className="w-full lg:w-48"><SelectValue /></SelectTrigger>
              <SelectContent>
                {sortOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}
              </SelectContent>
            </Select>
          </CardContent>
        </Card>

        {loading ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {Array.from({ length: 8 }, (_, index) => <Skeleton key={index} className="h-80 rounded-xl" />)}
          </div>
        ) : products.length === 0 ? (
          <Card><CardContent className="py-20 text-center text-muted-foreground">没有符合条件的商品</CardContent></Card>
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {products.map((product) => <ProductCard key={product.id} product={product} />)}
          </div>
        )}
      </main>
    </div>
  )
}
