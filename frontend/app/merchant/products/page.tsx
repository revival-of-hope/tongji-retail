"use client"

import { useEffect, useMemo, useState } from "react"
import { Pencil, Plus } from "lucide-react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"
import type {
  CategoryResponse,
  ProductListItem,
} from "@/lib/api/generated/types.gen"
import { api } from "@/lib/api/sdk"
import { flattenCategories } from "@/lib/categories"
import { currency, productStatusLabels } from "@/lib/format"

type ProductForm = {
  categoryId: string
  name: string
  description: string
  price: string
  stockQuantity: string
  imageUrls: string
}

const emptyForm: ProductForm = {
  categoryId: "",
  name: "",
  description: "",
  price: "",
  stockQuantity: "",
  imageUrls: "",
}

function parseImageUrls(value: string) {
  return value
    .split(/\n|,/)
    .map((item) => item.trim())
    .filter(Boolean)
}

function validateForm(form: ProductForm): string | null {
  if (!form.name.trim()) return "请输入商品名称"
  if (!form.categoryId) return "请选择商品分类"

  const price = Number(form.price)
  if (!Number.isFinite(price) || price <= 0) return "商品价格必须大于 0"

  const stock = Number(form.stockQuantity)
  if (!Number.isInteger(stock) || stock < 0) return "库存必须是非负整数"

  const imageUrls = parseImageUrls(form.imageUrls)
  if (imageUrls.length > 8) return "商品图片不能超过 8 张"
  for (const imageUrl of imageUrls) {
    if (imageUrl.length > 500) return "单个图片地址不能超过 500 个字符"
    try {
      const url = new URL(imageUrl)
      if (url.protocol !== "https:") return "图片必须使用 HTTPS 地址"
    } catch {
      return "存在格式不正确的图片地址"
    }
  }

  return null
}

export default function MerchantProductsPage() {
  const [products, setProducts] = useState<ProductListItem[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [editingId, setEditingId] = useState<number | null>(null)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState<ProductForm>(emptyForm)
  const [submitting, setSubmitting] = useState(false)

  const flatCategories = useMemo(() => flattenCategories(categories), [categories])

  const load = async () => {
    try {
      const [items, categoryItems] = await Promise.all([
        api.merchantProducts(),
        api.categories(),
      ])
      setProducts(items)
      setCategories(categoryItems)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "加载失败")
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const startCreate = () => {
    setEditingId(null)
    setForm(emptyForm)
    setOpen(true)
  }

  const startEdit = async (id: number) => {
    try {
      const product = await api.product(id)
      setEditingId(id)
      setForm({
        categoryId: String(product.categoryId),
        name: product.name,
        description: product.description ?? "",
        price: String(product.price),
        stockQuantity: String(product.stockQuantity),
        imageUrls: product.imageUrls.join("\n"),
      })
      setOpen(true)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "商品详情加载失败")
    }
  }

  const submit = async () => {
    const validationError = validateForm(form)
    if (validationError) {
      toast.error(validationError)
      return
    }

    const payload = {
      categoryId: Number(form.categoryId),
      name: form.name.trim(),
      description: form.description.trim() || null,
      price: Number(form.price),
      stockQuantity: Number(form.stockQuantity),
      imageUrls: parseImageUrls(form.imageUrls),
    }

    setSubmitting(true)
    try {
      if (editingId !== null) await api.updateProduct(editingId, payload)
      else await api.createProduct(payload)

      toast.success(editingId ? "商品已更新并重新进入审核" : "商品已提交审核")
      setOpen(false)
      await load()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "保存失败")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">商品管理</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            新建或修改商品后进入管理员审核流程。
          </p>
        </div>
        <Button onClick={startCreate}>
          <Plus />
          发布商品
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>商品</TableHead>
                <TableHead>分类</TableHead>
                <TableHead>价格</TableHead>
                <TableHead>库存</TableHead>
                <TableHead>状态</TableHead>
                <TableHead className="text-right">操作</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {products.map((product) => (
                <TableRow key={product.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      {product.mainImageUrl ? (
                        <img
                          src={product.mainImageUrl}
                          alt={product.name}
                          className="size-12 rounded-md object-cover"
                        />
                      ) : (
                        <div className="size-12 rounded-md bg-muted" />
                      )}
                      <div>
                        <p className="font-medium">{product.name}</p>
                        <p className="text-xs text-muted-foreground">
                          已售 {product.soldCount}
                        </p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>{product.categoryName}</TableCell>
                  <TableCell>{currency(product.price)}</TableCell>
                  <TableCell>{product.stockQuantity}</TableCell>
                  <TableCell>
                    <Badge variant={product.status === "Rejected" ? "destructive" : "secondary"}>
                      {productStatusLabels[product.status]}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => void startEdit(product.id)}
                    >
                      <Pencil />
                      编辑
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {products.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="py-12 text-center text-muted-foreground">
                    暂无商品
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editingId ? "编辑商品" : "发布商品"}</DialogTitle>
            <DialogDescription>
              图片地址每行一个，最多 8 张，必须使用 HTTPS；第一张作为主图。
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="product-name">商品名称</Label>
              <Input
                id="product-name"
                value={form.name}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                maxLength={200}
              />
            </div>
            <div className="space-y-2">
              <Label>分类</Label>
              <Select
                value={form.categoryId}
                onValueChange={(value) => setForm({ ...form, categoryId: value })}
              >
                <SelectTrigger>
                  <SelectValue placeholder="选择分类" />
                </SelectTrigger>
                <SelectContent>
                  {flatCategories.map((category) => (
                    <SelectItem key={category.id} value={String(category.id)}>
                      {category.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="product-price">价格</Label>
              <Input
                id="product-price"
                type="number"
                min="0.01"
                step="0.01"
                value={form.price}
                onChange={(event) => setForm({ ...form, price: event.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="product-stock">库存</Label>
              <Input
                id="product-stock"
                type="number"
                min="0"
                step="1"
                value={form.stockQuantity}
                onChange={(event) => setForm({ ...form, stockQuantity: event.target.value })}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="product-description">商品描述</Label>
              <Textarea
                id="product-description"
                value={form.description}
                onChange={(event) => setForm({ ...form, description: event.target.value })}
                maxLength={5000}
                rows={5}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="product-images">图片 URL</Label>
              <Textarea
                id="product-images"
                value={form.imageUrls}
                onChange={(event) => setForm({ ...form, imageUrls: event.target.value })}
                rows={5}
              />
            </div>
          </div>

          <DialogFooter>
            <Button disabled={submitting} onClick={() => void submit()}>
              {submitting ? "保存中…" : "保存"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
