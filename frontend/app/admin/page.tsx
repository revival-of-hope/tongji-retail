"use client"

import { useEffect, useState } from "react"
import { Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { toast } from "sonner"
import { StatCard } from "@/components/dashboard/stat-card"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { api } from "@/lib/api/sdk"
import type { CategorySalesPoint, DailySalesPoint, OverviewReport } from "@/lib/api/generated/types.gen"
import { currency } from "@/lib/format"

export default function AdminDashboardPage() {
  const [overview, setOverview] = useState<OverviewReport | null>(null)
  const [daily, setDaily] = useState<DailySalesPoint[]>([])
  const [categories, setCategories] = useState<CategorySalesPoint[]>([])
  useEffect(() => { void Promise.all([api.overview(), api.dailySales(), api.categorySales()]).then(([o, d, c]) => { setOverview(o); setDaily(d); setCategories(c) }).catch((error) => toast.error(error.message)) }, [])

  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">平台数据概览</h1><p className="mt-1 text-sm text-muted-foreground">销售额统计排除待支付与已取消订单。</p></div><div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><StatCard label="平台销售额" value={currency(overview?.totalSales ?? 0)} /><StatCard label="订单总数" value={overview?.totalOrders ?? 0} /><StatCard label="用户总数" value={overview?.totalUsers ?? 0} /><StatCard label="商品总数" value={overview?.totalProducts ?? 0} /></div><div className="grid gap-6 xl:grid-cols-2"><Card><CardHeader><CardTitle>近 30 天销售额</CardTitle></CardHeader><CardContent><div className="h-72"><ResponsiveContainer width="100%" height="100%"><LineChart data={daily}><XAxis dataKey="date" tickLine={false} axisLine={false} minTickGap={24} /><YAxis tickLine={false} axisLine={false} /><Tooltip formatter={(value) => currency(Number(value))} /><Line type="monotone" dataKey="sales" stroke="var(--primary)" strokeWidth={2} dot={false} /></LineChart></ResponsiveContainer></div></CardContent></Card><Card><CardHeader><CardTitle>分类销售额</CardTitle></CardHeader><CardContent><div className="h-72"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={categories} dataKey="sales" nameKey="categoryName" outerRadius={100} fill="var(--primary)" label /><Tooltip formatter={(value) => currency(Number(value))} /></PieChart></ResponsiveContainer></div></CardContent></Card></div><div className="grid gap-4 sm:grid-cols-3"><StatCard label="待审核商品" value={overview?.pendingProducts ?? 0} /><StatCard label="待审核商家" value={overview?.pendingMerchants ?? 0} /><StatCard label="开放工单" value={overview?.openTickets ?? 0} /></div></div>
}
