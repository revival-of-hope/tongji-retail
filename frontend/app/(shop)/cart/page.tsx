"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { Minus, Plus, Trash2 } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Checkbox } from "@/components/ui/checkbox"
import { Separator } from "@/components/ui/separator"
import { api } from "@/lib/api/sdk"
import type { CartItemResponse } from "@/lib/api/generated/types.gen"
import { currency } from "@/lib/format"

export default function CartPage() {
  const router = useRouter()
  const [items, setItems] = useState<CartItemResponse[]>([])
  const [selected, setSelected] = useState<number[]>([])
  const [loading, setLoading] = useState(true)

  const load = async () => {
    setLoading(true)
    try {
      const result = await api.cart()
      setItems(result)
      setSelected(result.filter((item) => item.available).map((item) => item.cartItemId))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "购物车加载失败")
    } finally {
      setLoading(false)
    }
  }
  useEffect(() => { void load() }, [])

  const selectedItems = useMemo(() => items.filter((item) => selected.includes(item.cartItemId)), [items, selected])
  const total = selectedItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
  const merchantCount = new Set(selectedItems.map((item) => item.storeName)).size

  const update = async (item: CartItemResponse, quantity: number) => {
    if (quantity < 1) return
    try {
      const updated = await api.updateCartItem(item.cartItemId, { quantity })
      setItems((current) => current.map((value) => value.cartItemId === item.cartItemId ? updated : value))
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "更新失败")
    }
  }

  const remove = async (id: number) => {
    try {
      await api.deleteCartItem(id)
      setItems((current) => current.filter((item) => item.cartItemId !== id))
      setSelected((current) => current.filter((value) => value !== id))
      toast.success("商品已删除")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "删除失败")
    }
  }

  return (
    <AuthGuard roles={["Customer"]}>
      <div className="min-h-screen bg-muted/20">
        <Navbar />
        <main className="mx-auto max-w-5xl px-4 py-8">
          <h1 className="text-2xl font-semibold">购物车</h1>
          <p className="mt-2 text-sm text-muted-foreground">为保证商家发货边界清晰，一次结算只能选择同一商家的商品。</p>
          <div className="mt-6 space-y-3">
            {!loading && items.length === 0 && <Card><CardContent className="py-20 text-center text-muted-foreground">购物车为空</CardContent></Card>}
            {items.map((item) => (
              <Card key={item.cartItemId}>
                <CardContent className="flex flex-col gap-4 p-4 md:flex-row md:items-center">
                  <Checkbox checked={selected.includes(item.cartItemId)} disabled={!item.available} onCheckedChange={() => setSelected((current) => current.includes(item.cartItemId) ? current.filter((id) => id !== item.cartItemId) : [...current, item.cartItemId])} aria-label={`选择${item.productName}`} />
                  <div className="size-20 overflow-hidden rounded-lg bg-muted">{item.mainImageUrl && <img src={item.mainImageUrl} alt={item.productName} className="h-full w-full object-cover" />}</div>
                  <div className="min-w-0 flex-1"><p className="font-medium">{item.productName}</p><p className="text-sm text-muted-foreground">{item.storeName}</p>{!item.available && <p className="text-sm text-destructive">已下架或库存不足</p>}</div>
                  <div className="font-semibold text-primary">{currency(item.unitPrice)}</div>
                  <div className="flex items-center gap-2"><Button variant="outline" size="icon" onClick={() => void update(item, item.quantity - 1)}><Minus /></Button><span className="w-8 text-center">{item.quantity}</span><Button variant="outline" size="icon" onClick={() => void update(item, item.quantity + 1)}><Plus /></Button></div>
                  <AlertDialog><AlertDialogTrigger asChild><Button variant="destructive" size="icon" aria-label="删除商品"><Trash2 /></Button></AlertDialogTrigger><AlertDialogContent><AlertDialogHeader><AlertDialogTitle>删除购物车商品？</AlertDialogTitle><AlertDialogDescription>将从购物车中移除“{item.productName}”。</AlertDialogDescription></AlertDialogHeader><AlertDialogFooter><AlertDialogCancel>取消</AlertDialogCancel><AlertDialogAction onClick={() => void remove(item.cartItemId)}>确认删除</AlertDialogAction></AlertDialogFooter></AlertDialogContent></AlertDialog>
                </CardContent>
              </Card>
            ))}
          </div>
          {items.length > 0 && <Card className="sticky bottom-4 mt-6 shadow-lg"><CardContent className="p-4"><div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"><div><span>已选 {selected.length} 件，合计 <strong className="text-xl text-primary">{currency(total)}</strong></span>{merchantCount > 1 && <p className="mt-1 text-sm text-destructive">当前选择包含多个商家，请仅保留一个商家。</p>}</div><Button disabled={selected.length === 0 || merchantCount !== 1} onClick={() => router.push(`/orders/checkout?items=${selected.join(",")}`)}>去结算</Button></div><Separator className="mt-4" /><p className="mt-3 text-xs text-muted-foreground">库存将在支付成功时扣减；创建订单后 30 分钟未支付将失效。</p></CardContent></Card>}
        </main>
      </div>
    </AuthGuard>
  )
}
