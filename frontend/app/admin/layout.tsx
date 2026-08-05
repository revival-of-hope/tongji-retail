"use client"

import { AuthGuard } from "@/components/auth/auth-guard"
import { DashboardShell } from "@/components/layout/dashboard-shell"

const items = [
  { href: "/admin", label: "数据概览" },
  { href: "/admin/products", label: "商品审核" },
  { href: "/admin/merchants", label: "商家审核" },
  { href: "/admin/users", label: "用户管理" },
  { href: "/admin/orders", label: "订单管理" },
  { href: "/admin/tickets", label: "客服工单" },
]

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AuthGuard roles={["Admin"]}><DashboardShell title="管理员后台" items={items}>{children}</DashboardShell></AuthGuard>
}
