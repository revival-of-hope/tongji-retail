import type { MerchantStatus, OrderStatus, ProductStatus, TicketStatus, UserRole } from "@/lib/api/generated/types.gen"

export const roleLabels: Record<UserRole, string> = { Admin: "管理员", Customer: "顾客", Merchant: "商家", CustomerService: "客服" }
export const orderStatusLabels: Record<OrderStatus, string> = { PendingPayment: "待支付", PendingShipment: "待发货", Shipped: "已发货", Completed: "已完成", Cancelled: "已取消" }
export const productStatusLabels: Record<ProductStatus, string> = { PendingReview: "待审核", OnSale: "已上架", OffShelf: "已下架", Rejected: "已拒绝" }
export const merchantStatusLabels: Record<MerchantStatus, string> = { Pending: "待审核", Approved: "已通过", Rejected: "已拒绝" }
export const ticketStatusLabels: Record<TicketStatus, string> = { Pending: "待处理", Processing: "处理中", Resolved: "已解决", Closed: "已关闭" }
export const currency = (value: number) => new Intl.NumberFormat("zh-CN", { style: "currency", currency: "CNY" }).format(value)
export const dateTime = (value: string) => new Intl.DateTimeFormat("zh-CN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value))
