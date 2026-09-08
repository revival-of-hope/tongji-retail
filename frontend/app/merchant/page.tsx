"use client"

import { useEffect, useState } from "react"
import { Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { toast } from "sonner"
import { StatCard } from "@/components/dashboard/stat-card"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { MerchantReport } from "@/lib/api/generated/types.gen"
import { currency } from "@/lib/format"

export default function MerchantDashboardPage() {
  const [report, setReport] = useState<MerchantReport | null>(null)
  useEffect(() => { void api.merchantReport().then(setReport).catch((error) => toast.error(error.message)) }, [])

  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">经营概览</h1><p className="mt-1 text-sm text-muted-foreground">统计本店已支付、已发货和已完成订单。</p></div><div className="grid gap-4 sm:grid-cols-2"><StatCard label="累计销售额" value={currency(report?.totalSales ?? 0)} /><StatCard label="累计订单数" value={report?.totalOrders ?? 0} /></div><Card><CardHeader><CardTitle>近 30 天销售额</CardTitle></CardHeader><CardContent><div className="h-72"><ResponsiveContainer width="100%" height="100%"><LineChart data={report?.dailySales ?? []}><XAxis dataKey="date" tickLine={false} axisLine={false} minTickGap={24} /><YAxis tickLine={false} axisLine={false} /><Tooltip formatter={(value) => currency(Number(value))} /><Line type="monotone" dataKey="sales" stroke="var(--primary)" strokeWidth={2} dot={false} /></LineChart></ResponsiveContainer></div></CardContent></Card><Card><CardHeader><CardTitle>商品销售排行</CardTitle></CardHeader><CardContent><Table><TableHeader><TableRow><TableHead>商品</TableHead><TableHead className="text-right">销量</TableHead><TableHead className="text-right">销售额</TableHead></TableRow></TableHeader><TableBody>{(report?.topProducts ?? []).map((item) => <TableRow key={item.productId}><TableCell className="font-medium">{item.productName}</TableCell><TableCell className="text-right">{item.quantity}</TableCell><TableCell className="text-right">{currency(item.sales)}</TableCell></TableRow>)}{report?.topProducts.length === 0 && <TableRow><TableCell colSpan={3} className="py-10 text-center text-muted-foreground">暂无销售数据</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
