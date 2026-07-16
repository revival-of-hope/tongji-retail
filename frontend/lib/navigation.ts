import type { UserRole } from "@/lib/api/generated/types.gen"

export type NavigationItem = { href: string; label: string }

export const workspaceRoutes: Partial<Record<UserRole, string>> = {
  Admin: "/admin",
  Merchant: "/merchant",
  CustomerService: "/cs",
}

export function getNavigationItems(role?: UserRole): NavigationItem[] {
  const items: NavigationItem[] = [{ href: "/", label: "商城" }]

  if (role === "Customer") {
    items.push(
      { href: "/cart", label: "购物车" },
      { href: "/orders", label: "订单" },
      { href: "/tickets", label: "客服工单" },
      { href: "/merchant/apply", label: "商家入驻" },
    )
  }

  const workspace = role ? workspaceRoutes[role] : undefined
  if (workspace) items.push({ href: workspace, label: "工作台" })

  return items
}
