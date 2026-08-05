"use client"

import { Suspense, useEffect, useMemo, useState } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { CartItemResponse } from "@/lib/api/generated/types.gen"
import { currency } from "@/lib/format"

function CheckoutContent() {
  const router = useRouter()
  const search = useSearchParams()
  const itemParam = search.get("items") ?? ""
  const ids = useMemo(() => itemParam.split(",").map(Number).filter((value) => Number.isInteger(value) && value > 0), [itemParam])
  const [items, setItems] = useState<CartItemResponse[]>([])
  const [address, setAddress] = useState("")
  const [remark, setRemark] = useState("")
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    void api.cart().then((all) => setItems(all.filter((item) => ids.includes(item.cartItemId)))).catch((error) => toast.error(error.message))
  }, [ids])

  const total = items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
  const merchantCount = new Set(items.map((item) => item.storeName)).size

  const submit = async () => {
    setSubmitting(true)
    try {
      const order = await api.createOrder({ cartItemIds: ids, shippingAddress: address.trim(), remark: remark.trim() || null })
      toast.success("订单已创建")
      router.replace(`/orders/${order.id}/pay`)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "下单失败")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="mx-auto max-w-4xl space-y-6 px-4 py-8">
      <div><h1 className="text-2xl font-semibold">确认订单</h1><p className="mt-1 text-sm text-muted-foreground">价格与商品名称将在订单项中保存快照。</p></div>
      <Card>
        <CardHeader><CardTitle>商品清单</CardTitle></CardHeader>
        <CardContent className="space-y-4">{items.map((item) => <div key={item.cartItemId} className="flex justify-between gap-4 border-b pb-4 last:border-0 last:pb-0"><div><strong>{item.productName}</strong><p className="text-sm text-muted-foreground">{item.storeName} · 数量 {item.quantity}</p></div><span>{currency(item.unitPrice * item.quantity)}</span></div>)}{items.length === 0 && <p className="text-sm text-muted-foreground">未找到可结算商品，请返回购物车重新选择。</p>}</CardContent>
      </Card>
      <Card>
        <CardHeader><CardTitle>配送信息</CardTitle></CardHeader>
        <CardContent className="space-y-5"><div className="space-y-2"><Label htmlFor="address">收货地址</Label><Textarea id="address" value={address} onChange={(event) => setAddress(event.target.value)} placeholder="请输入完整收货地址" maxLength={500} /></div><div className="space-y-2"><Label htmlFor="remark">订单备注</Label><Textarea id="remark" value={remark} onChange={(event) => setRemark(event.target.value)} placeholder="选填，最多 500 字" maxLength={500} /></div></CardContent>
      </Card>
      <Card><CardContent className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between"><div><span>订单金额 <strong className="text-2xl text-primary">{currency(total)}</strong></span>{merchantCount > 1 && <p className="text-sm text-destructive">所选商品不属于同一商家。</p>}</div><Button disabled={submitting || items.length === 0 || !address.trim() || merchantCount !== 1} onClick={() => void submit()}>{submitting ? "提交中…" : "提交订单"}</Button></CardContent></Card>
    </main>
  )
}

export default function CheckoutPage() {
  return <AuthGuard roles={["Customer"]}><div className="min-h-screen bg-muted/20"><Navbar /><Suspense fallback={<main className="mx-auto max-w-4xl space-y-4 px-4 py-8"><Skeleton className="h-10 w-48" /><Skeleton className="h-48" /><Skeleton className="h-56" /></main>}><CheckoutContent /></Suspense></div></AuthGuard>
}
