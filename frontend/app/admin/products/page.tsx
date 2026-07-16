"use client"

import { useEffect, useState } from "react"
import { Check, X } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { ProductListItem } from "@/lib/api/generated/types.gen"
import { currency } from "@/lib/format"

export default function AdminProductsPage() {
  const [products, setProducts] = useState<ProductListItem[]>([])
  const load = async () => { try { setProducts(await api.pendingProducts()) } catch (error) { toast.error(error instanceof Error ? error.message : "加载失败") } }
  useEffect(() => { void load() }, [])
  const review = async (id: number, approved: boolean) => { try { await api.reviewProduct(id, { approved }); toast.success(approved ? "商品已通过" : "商品已拒绝"); await load() } catch (error) { toast.error(error instanceof Error ? error.message : "审核失败") } }
  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">商品审核</h1><p className="mt-1 text-sm text-muted-foreground">审核通过后商品才会出现在商城。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>商品</TableHead><TableHead>店铺</TableHead><TableHead>分类</TableHead><TableHead>价格 / 库存</TableHead><TableHead className="text-right">审核</TableHead></TableRow></TableHeader><TableBody>{products.map((product) => <TableRow key={product.id}><TableCell><div className="flex items-center gap-3">{product.mainImageUrl ? <img src={product.mainImageUrl} alt={product.name} className="size-14 rounded-md object-cover" /> : <div className="size-14 rounded-md bg-muted" />}<strong>{product.name}</strong></div></TableCell><TableCell>{product.storeName}</TableCell><TableCell>{product.categoryName}</TableCell><TableCell>{currency(product.price)} / {product.stockQuantity}</TableCell><TableCell><div className="flex justify-end gap-2"><Button size="sm" onClick={() => void review(product.id, true)}><Check />通过</Button><Button size="sm" variant="destructive" onClick={() => void review(product.id, false)}><X />拒绝</Button></div></TableCell></TableRow>)}{products.length === 0 && <TableRow><TableCell colSpan={5} className="py-12 text-center text-muted-foreground">没有待审核商品</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
