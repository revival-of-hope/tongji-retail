"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Search, SlidersHorizontal, X } from "lucide-react"
import { toast } from "sonner"
import { Navbar } from "@/components/layout/navbar"
import { ProductCard } from "@/components/product/product-card"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import { api } from "@/lib/api/sdk"
import { flattenCategories } from "@/lib/categories"
import type {
  CategoryResponse,
  GetProductsData,
  ProductListItem,
} from "@/lib/api/generated/types.gen"

type SortBy = NonNullable<NonNullable<GetProductsData["query"]>["sortBy"]>
const PAGE_SIZE = 12
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
  const [minPrice, setMinPrice] = useState("")
  const [maxPrice, setMaxPrice] = useState("")
  const [submittedMinPrice, setSubmittedMinPrice] = useState<number | undefined>()
  const [submittedMaxPrice, setSubmittedMaxPrice] = useState<number | undefined>()
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [sortBy, setSortBy] = useState<SortBy>("newest")
  const [pageIndex, setPageIndex] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [loading, setLoading] = useState(true)

  const flatCategories = useMemo(() => flattenCategories(categories), [categories])

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.products({
        pageIndex,
        pageSize: PAGE_SIZE,
        keyword: submittedKeyword || undefined,
        minPrice: submittedMinPrice,
        maxPrice: submittedMaxPrice,
        categoryId,
        sortBy,
      })
      setProducts(result.items)
      setTotalCount(result.totalCount)
      setTotalPages(result.totalPages)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "商品加载失败")
    } finally {
      setLoading(false)
    }
  }, [categoryId, pageIndex, sortBy, submittedKeyword, submittedMaxPrice, submittedMinPrice])

  useEffect(() => {
    void api
      .categories()
      .then(setCategories)
      .catch(() => toast.error("分类加载失败"))
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const submitFilters = () => {
    const parsedMin = minPrice.trim() === "" ? undefined : Number(minPrice)
    const parsedMax = maxPrice.trim() === "" ? undefined : Number(maxPrice)
    if (parsedMin !== undefined && (!Number.isFinite(parsedMin) || parsedMin < 0)) {
      toast.error("最低价格必须是非负数")
      return
    }
    if (parsedMax !== undefined && (!Number.isFinite(parsedMax) || parsedMax < 0)) {
      toast.error("最高价格必须是非负数")
      return
    }
    if (parsedMin !== undefined && parsedMax !== undefined && parsedMin > parsedMax) {
      toast.error("最低价格不能高于最高价格")
      return
    }

    setSubmittedKeyword(keyword.trim())
    setSubmittedMinPrice(parsedMin)
    setSubmittedMaxPrice(parsedMax)
    setPageIndex(1)
  }

  const resetFilters = () => {
    setKeyword("")
    setSubmittedKeyword("")
    setMinPrice("")
    setMaxPrice("")
    setSubmittedMinPrice(undefined)
    setSubmittedMaxPrice(undefined)
    setCategoryId(undefined)
    setSortBy("newest")
    setPageIndex(1)
  }

  const hasFilters = Boolean(
    submittedKeyword ||
      submittedMinPrice !== undefined ||
      submittedMaxPrice !== undefined ||
      categoryId !== undefined ||
      sortBy !== "newest",
  )

  return (
    <div className="min-h-screen bg-muted/20">
      <Navbar />
      <section className="border-b bg-background">
        <div className="mx-auto max-w-7xl px-4 py-12">
          <p className="text-sm font-medium text-primary">商品零售管理系统</p>
          <h1 className="mt-2 max-w-3xl text-3xl font-semibold tracking-tight md:text-5xl">
            欢迎选购
          </h1>
          <form
            className="mt-8 flex max-w-2xl gap-2"
            onSubmit={(event) => {
              event.preventDefault()
              submitFilters()
            }}
          >
            <div className="relative flex-1">
              <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="搜索商品名称或描述"
                maxLength={200}
                className="pl-9"
              />
            </div>
            <Button type="submit">搜索</Button>
          </form>
        </div>
      </section>

      <main className="mx-auto max-w-7xl px-4 py-8">
        <Card className="mb-6">
          <CardContent className="space-y-4 p-4">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
              <div className="flex-1 space-y-2">
                <label className="text-sm font-medium">商品分类</label>
                <Select
                  value={categoryId === undefined ? "all" : String(categoryId)}
                  onValueChange={(value) => {
                    setCategoryId(value === "all" ? undefined : Number(value))
                    setPageIndex(1)
                  }}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">全部分类</SelectItem>
                    {flatCategories.map((category) => (
                      <SelectItem key={category.id} value={String(category.id)}>
                        {category.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="grid flex-1 grid-cols-2 gap-3">
                <div className="space-y-2">
                  <label htmlFor="min-price" className="text-sm font-medium">
                    最低价格
                  </label>
                  <Input
                    id="min-price"
                    type="number"
                    min="0"
                    step="0.01"
                    value={minPrice}
                    onChange={(event) => setMinPrice(event.target.value)}
                    placeholder="不限"
                  />
                </div>
                <div className="space-y-2">
                  <label htmlFor="max-price" className="text-sm font-medium">
                    最高价格
                  </label>
                  <Input
                    id="max-price"
                    type="number"
                    min="0"
                    step="0.01"
                    value={maxPrice}
                    onChange={(event) => setMaxPrice(event.target.value)}
                    placeholder="不限"
                  />
                </div>
              </div>

              <div className="flex-1 space-y-2">
                <label className="text-sm font-medium">排序方式</label>
                <Select
                  value={sortBy}
                  onValueChange={(value) => {
                    setSortBy(value as SortBy)
                    setPageIndex(1)
                  }}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {sortOptions.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex gap-2">
                <Button onClick={submitFilters}>
                  <SlidersHorizontal />
                  应用筛选
                </Button>
                {hasFilters && (
                  <Button variant="outline" onClick={resetFilters}>
                    <X />
                    重置
                  </Button>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="mb-4 flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
          <span>共 {totalCount} 件商品</span>
          {totalPages > 0 && (
            <span>
              第 {pageIndex} / {totalPages} 页
            </span>
          )}
        </div>

        {loading ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {Array.from({ length: PAGE_SIZE }, (_, index) => (
              <Skeleton key={index} className="h-80 rounded-xl" />
            ))}
          </div>
        ) : products.length === 0 ? (
          <Card>
            <CardContent className="py-20 text-center text-muted-foreground">
              没有符合条件的商品
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {products.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="mt-8 flex items-center justify-center gap-3">
            <Button
              variant="outline"
              disabled={loading || pageIndex <= 1}
              onClick={() => setPageIndex((page) => Math.max(1, page - 1))}
            >
              上一页
            </Button>
            <span className="text-sm text-muted-foreground">
              {pageIndex} / {totalPages}
            </span>
            <Button
              variant="outline"
              disabled={loading || pageIndex >= totalPages}
              onClick={() => setPageIndex((page) => Math.min(totalPages, page + 1))}
            >
              下一页
            </Button>
          </div>
        )}
      </main>
    </div>
  )
}
