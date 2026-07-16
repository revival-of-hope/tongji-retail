"use client"

import { useEffect, useState } from "react"
import { Pencil, Plus } from "lucide-react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { CategoryResponse, ProductListItem } from "@/lib/api/generated/types.gen"
import { currency, productStatusLabels } from "@/lib/format"

type ProductForm = { categoryId: string; name: string; description: string; price: string; stockQuantity: string; imageUrls: string }
const emptyForm: ProductForm = { categoryId: "", name: "", description: "", price: "", stockQuantity: "", imageUrls: "" }

export default function MerchantProductsPage() {
  const [products, setProducts] = useState<ProductListItem[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [editingId, setEditingId] = useState<number | null>(null)
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState<ProductForm>(emptyForm)
  const [submitting, setSubmitting] = useState(false)

  const load = async () => {
    try {
      const [items, categoryItems] = await Promise.all([api.merchantProducts(), api.categories()])
      setProducts(items)
      setCategories(categoryItems)
    } catch (error) { toast.error(error instanceof Error ? error.message : "加载失败") }
  }
  useEffect(() => { void load() }, [])

  const startCreate = () => { setEditingId(null); setForm(emptyForm); setOpen(true) }
  const startEdit = async (id: number) => {
    try {
      const product = await api.product(id)
      setEditingId(id)
      setForm({ categoryId: String(product.categoryId), name: product.name, description: product.description ?? "", price: String(product.price), stockQuantity: String(product.stockQuantity), imageUrls: product.imageUrls.join("\n") })
      setOpen(true)
    } catch (error) { toast.error(error instanceof Error ? error.message : "商品详情加载失败") }
  }

  const submit = async () => {
    const payload = {
      categoryId: Number(form.categoryId),
      name: form.name.trim(),
      description: form.description.trim() || null,
      price: Number(form.price),
      stockQuantity: Number(form.stockQuantity),
      imageUrls: form.imageUrls.split(/\n|,/).map((item) => item.trim()).filter(Boolean),
    }
    setSubmitting(true)
    try {
      if (editingId) await api.updateProduct(editingId, payload)
      else await api.createProduct(payload)
      toast.success(editingId ? "商品已更新并重新进入审核" : "商品已提交审核")
      setOpen(false)
      await load()
    } catch (error) { toast.error(error instanceof Error ? error.message : "保存失败") }
    finally { setSubmitting(false) }
  }

  return <div className="space-y-6"><div className="flex flex-wrap items-center justify-between gap-3"><div><h1 className="text-2xl font-semibold">商品管理</h1><p className="mt-1 text-sm text-muted-foreground">新建或修改商品后进入管理员审核流程。</p></div><Button onClick={startCreate}><Plus />发布商品</Button></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>商品</TableHead><TableHead>分类</TableHead><TableHead>价格</TableHead><TableHead>库存</TableHead><TableHead>状态</TableHead><TableHead className="text-right">操作</TableHead></TableRow></TableHeader><TableBody>{products.map((product) => <TableRow key={product.id}><TableCell><div className="flex items-center gap-3">{product.mainImageUrl ? <img src={product.mainImageUrl} alt={product.name} className="size-12 rounded-md object-cover" /> : <div className="size-12 rounded-md bg-muted" />}<div><p className="font-medium">{product.name}</p><p className="text-xs text-muted-foreground">已售 {product.soldCount}</p></div></div></TableCell><TableCell>{product.categoryName}</TableCell><TableCell>{currency(product.price)}</TableCell><TableCell>{product.stockQuantity}</TableCell><TableCell><Badge variant={product.status === "Rejected" ? "destructive" : "secondary"}>{productStatusLabels[product.status]}</Badge></TableCell><TableCell className="text-right"><Button variant="outline" size="sm" onClick={() => void startEdit(product.id)}><Pencil />编辑</Button></TableCell></TableRow>)}{products.length === 0 && <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">暂无商品</TableCell></TableRow>}</TableBody></Table></CardContent></Card><Dialog open={open} onOpenChange={setOpen}><DialogTrigger asChild><span /></DialogTrigger><DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl"><DialogHeader><DialogTitle>{editingId ? "编辑商品" : "发布商品"}</DialogTitle><DialogDescription>图片地址每行一个，最多 8 张；第一张作为主图。</DialogDescription></DialogHeader><div className="grid gap-4 sm:grid-cols-2"><div className="space-y-2 sm:col-span-2"><Label>商品名称</Label><Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} maxLength={200} /></div><div className="space-y-2"><Label>分类</Label><Select value={form.categoryId} onValueChange={(value) => setForm({ ...form, categoryId: value })}><SelectTrigger><SelectValue placeholder="选择分类" /></SelectTrigger><SelectContent>{categories.map((category) => <SelectItem key={category.id} value={String(category.id)}>{category.name}</SelectItem>)}</SelectContent></Select></div><div className="space-y-2"><Label>价格</Label><Input type="number" min="0.01" step="0.01" value={form.price} onChange={(event) => setForm({ ...form, price: event.target.value })} /></div><div className="space-y-2"><Label>库存</Label><Input type="number" min="0" step="1" value={form.stockQuantity} onChange={(event) => setForm({ ...form, stockQuantity: event.target.value })} /></div><div className="space-y-2 sm:col-span-2"><Label>商品描述</Label><Textarea value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} rows={5} /></div><div className="space-y-2 sm:col-span-2"><Label>图片 URL</Label><Textarea value={form.imageUrls} onChange={(event) => setForm({ ...form, imageUrls: event.target.value })} rows={5} /></div></div><DialogFooter><Button disabled={submitting || !form.name.trim() || !form.categoryId || Number(form.price) <= 0 || Number(form.stockQuantity) < 0} onClick={() => void submit()}>{submitting ? "保存中…" : "保存"}</Button></DialogFooter></DialogContent></Dialog></div>
}
