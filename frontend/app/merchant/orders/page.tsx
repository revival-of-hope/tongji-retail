"use client"

import { useEffect, useState } from "react"
import { Truck } from "lucide-react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { OrderSummary } from "@/lib/api/generated/types.gen"
import { currency, dateTime, orderStatusLabels } from "@/lib/format"

export default function MerchantOrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const load = async () => { try { setOrders(await api.merchantOrders()) } catch (error) { toast.error(error instanceof Error ? error.message : "订单加载失败") } }
  useEffect(() => { void load() }, [])
  const ship = async (id: number) => { try { await api.shipOrder(id); toast.success("订单已发货"); await load() } catch (error) { toast.error(error instanceof Error ? error.message : "发货失败") } }

  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">订单发货</h1><p className="mt-1 text-sm text-muted-foreground">每个订单仅包含本店商品，可按订单整体发货。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>订单</TableHead><TableHead>商品</TableHead><TableHead>收货地址</TableHead><TableHead>金额</TableHead><TableHead>状态</TableHead><TableHead className="text-right">操作</TableHead></TableRow></TableHeader><TableBody>{orders.map((order) => <TableRow key={order.id}><TableCell><p className="font-medium">{order.orderNo}</p><p className="text-xs text-muted-foreground">{dateTime(order.createdAt)}</p></TableCell><TableCell>{order.items.map((item) => `${item.productName} × ${item.quantity}`).join("；")}</TableCell><TableCell className="max-w-64 whitespace-normal">{order.shippingAddress}</TableCell><TableCell>{currency(order.totalAmount)}</TableCell><TableCell><Badge variant="secondary">{orderStatusLabels[order.status]}</Badge></TableCell><TableCell className="text-right">{order.status === "PendingShipment" ? <Button size="sm" onClick={() => void ship(order.id)}><Truck />确认发货</Button> : "—"}</TableCell></TableRow>)}{orders.length === 0 && <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">暂无订单</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
