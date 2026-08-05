"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { OrderSummary } from "@/lib/api/generated/types.gen"
import { currency, dateTime, orderStatusLabels } from "@/lib/format"

export default function AdminOrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  useEffect(() => { void api.adminOrders().then(setOrders).catch((error) => toast.error(error.message)) }, [])
  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">订单管理</h1><p className="mt-1 text-sm text-muted-foreground">平台订单只读总览，业务状态由顾客与商家操作流转。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>订单号</TableHead><TableHead>店铺 / 商品</TableHead><TableHead>配送地址</TableHead><TableHead>金额</TableHead><TableHead>状态</TableHead><TableHead>创建时间</TableHead></TableRow></TableHeader><TableBody>{orders.map((order) => <TableRow key={order.id}><TableCell className="font-medium">{order.orderNo}</TableCell><TableCell className="max-w-72 whitespace-normal"><p>{order.items[0]?.storeName ?? "—"}</p><p className="text-xs text-muted-foreground">{order.items.map((item) => `${item.productName} × ${item.quantity}`).join("；")}</p></TableCell><TableCell className="max-w-64 whitespace-normal">{order.shippingAddress}</TableCell><TableCell>{currency(order.totalAmount)}</TableCell><TableCell><Badge variant={order.status === "Cancelled" ? "destructive" : "secondary"}>{orderStatusLabels[order.status]}</Badge></TableCell><TableCell>{dateTime(order.createdAt)}</TableCell></TableRow>)}{orders.length === 0 && <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">暂无订单</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
