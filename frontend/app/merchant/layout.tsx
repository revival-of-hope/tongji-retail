"use client"

import { usePathname } from "next/navigation"
import { AuthGuard } from "@/components/auth/auth-guard"
import { DashboardShell } from "@/components/layout/dashboard-shell"

const items = [
  { href: "/merchant", label: "经营概览" },
  { href: "/merchant/products", label: "商品管理" },
  { href: "/merchant/orders", label: "订单发货" },
]

export default function MerchantLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()

  // 商家入驻属于顾客流程，不能继承商家工作台的权限守卫和侧边栏。
  if (pathname === "/merchant/apply") {
    return children
  }

  return (
    <AuthGuard roles={["Merchant"]}>
      <DashboardShell title="商家工作台" items={items}>
        {children}
      </DashboardShell>
    </AuthGuard>
  )
}
