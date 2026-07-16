import { describe, expect, it } from "vitest"
import { getNavigationItems, workspaceRoutes } from "@/lib/navigation"

describe("role navigation", () => {
  it("does not expose a customer workspace", () => {
    expect(workspaceRoutes.Customer).toBeUndefined()
    expect(getNavigationItems("Customer")).toEqual([
      { href: "/", label: "商城" },
      { href: "/cart", label: "购物车" },
      { href: "/orders", label: "订单" },
      { href: "/tickets", label: "客服工单" },
      { href: "/merchant/apply", label: "商家入驻" },
    ])
  })

  it("keeps workspaces for operational roles", () => {
    expect(getNavigationItems("Merchant")).toContainEqual({ href: "/merchant", label: "工作台" })
    expect(getNavigationItems("CustomerService")).toContainEqual({ href: "/cs", label: "工作台" })
    expect(getNavigationItems("Admin")).toContainEqual({ href: "/admin", label: "工作台" })
  })
})
