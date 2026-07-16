"use client"

import Link from "next/link"
import { useEffect, useState } from "react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { OrderItemResponse, OrderSummary } from "@/lib/api/generated/types.gen"
import { currency, dateTime, orderStatusLabels } from "@/lib/format"

export default function OrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [reviewTarget, setReviewTarget] = useState<{ order: OrderSummary; item: OrderItemResponse } | null>(null)
  const [rating, setRating] = useState("5")
  const [comment, setComment] = useState("")

  const load = async () => {
    try { setOrders(await api.orders()) } catch (error) { toast.error(error instanceof Error ? error.message : "订单加载失败") }
  }
  useEffect(() => { void load() }, [])

  const action = async (id: number, type: "complete" | "cancel") => {
    try {
      if (type === "complete") await api.completeOrder(id)
      else await api.cancelOrder(id)
      toast.success(type === "complete" ? "已确认收货" : "订单已取消")
      await load()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "操作失败")
    }
  }

  const submitReview = async () => {
    if (!reviewTarget) return
    try {
      await api.createReview(reviewTarget.item.productId, { orderId: reviewTarget.order.id, rating: Number(rating), comment: comment.trim() || null })
      toast.success("评价已提交")
      setReviewTarget(null)
      setComment("")
      setRating("5")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "评价失败")
    }
  }

  return (
    <AuthGuard roles={["Customer"]}>
      <div className="min-h-screen bg-muted/20">
        <Navbar />
        <main className="mx-auto max-w-5xl px-4 py-8">
          <h1 className="text-2xl font-semibold">我的订单</h1>
          <div className="mt-6 space-y-4">
            {orders.length === 0 && <Card><CardContent className="py-20 text-center text-muted-foreground">暂无订单</CardContent></Card>}
            {orders.map((order) => (
              <Card key={order.id}>
                <CardHeader className="flex-row flex-wrap items-center justify-between gap-3 border-b"><div><p className="font-semibold">{order.orderNo}</p><p className="text-xs text-muted-foreground">{dateTime(order.createdAt)}</p></div><Badge variant={order.status === "Cancelled" ? "destructive" : "secondary"}>{orderStatusLabels[order.status]}</Badge></CardHeader>
                <CardContent className="space-y-4 p-5">
                  {order.items.map((item) => <div key={item.id} className="flex flex-wrap items-center justify-between gap-3 border-b pb-3 last:border-0"><div><Link href={`/products/${item.productId}`} className="font-medium hover:underline">{item.productName}</Link><p className="text-sm text-muted-foreground">{item.storeName} · 数量 {item.quantity}</p></div><div className="flex items-center gap-3"><span>{currency(item.subTotal)}</span>{order.status === "Completed" && <Button size="sm" variant="outline" onClick={() => setReviewTarget({ order, item })}>评价</Button>}</div></div>)}
                  <div className="flex flex-wrap items-center justify-between gap-3 pt-2"><div><p className="text-sm text-muted-foreground">配送至：{order.shippingAddress}</p><span>合计 <strong className="text-lg text-primary">{currency(order.totalAmount)}</strong></span></div><div className="flex gap-2">{order.status === "PendingPayment" && <Button asChild><Link href={`/orders/${order.id}/pay`}>去支付</Link></Button>}{(order.status === "PendingPayment" || order.status === "PendingShipment") && <AlertDialog><AlertDialogTrigger asChild><Button variant="destructive">取消订单</Button></AlertDialogTrigger><AlertDialogContent><AlertDialogHeader><AlertDialogTitle>取消订单？</AlertDialogTitle><AlertDialogDescription>{order.status === "PendingShipment" ? "已支付订单取消后将恢复库存，本项目以失败支付状态模拟退款。" : "未支付订单将直接取消。"}</AlertDialogDescription></AlertDialogHeader><AlertDialogFooter><AlertDialogCancel>返回</AlertDialogCancel><AlertDialogAction onClick={() => void action(order.id, "cancel")}>确认取消</AlertDialogAction></AlertDialogFooter></AlertDialogContent></AlertDialog>}{order.status === "Shipped" && <Button onClick={() => void action(order.id, "complete")}>确认收货</Button>}</div></div>
                </CardContent>
              </Card>
            ))}
          </div>
        </main>
        <Dialog open={reviewTarget !== null} onOpenChange={(open) => !open && setReviewTarget(null)}><DialogContent><DialogHeader><DialogTitle>评价商品</DialogTitle><DialogDescription>{reviewTarget?.item.productName}</DialogDescription></DialogHeader><div className="space-y-4"><div className="space-y-2"><Label>评分</Label><Select value={rating} onValueChange={setRating}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent>{[5,4,3,2,1].map((value) => <SelectItem key={value} value={String(value)}>{value} 星</SelectItem>)}</SelectContent></Select></div><div className="space-y-2"><Label htmlFor="review-comment">评价内容</Label><Textarea id="review-comment" value={comment} onChange={(event) => setComment(event.target.value)} maxLength={1000} /></div></div><DialogFooter><Button onClick={() => void submitReview()}>提交评价</Button></DialogFooter></DialogContent></Dialog>
      </div>
    </AuthGuard>
  )
}
