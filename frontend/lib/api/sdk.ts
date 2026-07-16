import { client } from "./generated/client.gen"
import * as generated from "./generated/sdk.gen"
import type {
  AddCartItemRequest,
  ApplyMerchantRequest,
  AuthResponse,
  CartItemResponse,
  CategoryResponse,
  CategorySalesPoint,
  CreateOrderRequest,
  CreateProductRequest,
  CreateReviewRequest,
  CreateTicketRequest,
  DailySalesPoint,
  LoginRequest,
  MerchantReport,
  GetProductsData,
  MerchantSummary,
  OrderDetail,
  OrderSummary,
  OverviewReport,
  PayOrderRequest,
  ProductDetail,
  ProductListItem,
  PagedProductListItem,
  ProductReviewResponse,
  RegisterRequest,
  ReplyTicketRequest,
  ReviewMerchantRequest,
  ReviewProductRequest,
  TicketResponse,
  UpdateCartItemRequest,
  UpdateProductRequest,
  UserSummary,
} from "./generated/types.gen"

export class ApiError extends Error {
  constructor(message: string, public readonly status = 0) {
    super(message)
    this.name = "ApiError"
  }
}

client.setConfig({
  baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080",
  auth: () => (typeof window === "undefined" ? undefined : window.localStorage.getItem("retail-access-token") ?? undefined),
})

type Envelope<T> = { code: number; message: string; data?: T | null }
type ClientResult<T> = { data?: Envelope<T>; error?: Envelope<unknown>; response?: Response }

async function unwrap<T>(request: Promise<unknown>): Promise<T> {
  const result = (await request) as ClientResult<T>
  if (result.error) throw new ApiError(result.error.message || "请求失败", result.response?.status)
  const envelope = result.data
  if (!envelope) throw new ApiError("接口未返回响应", result.response?.status)
  if (envelope.data === null || envelope.data === undefined) throw new ApiError(envelope.message || "接口未返回数据", result.response?.status)
  return envelope.data
}

export const api = {
  register: (body: RegisterRequest) => unwrap<AuthResponse>(generated.register({ body })),
  login: (body: LoginRequest) => unwrap<AuthResponse>(generated.login({ body })),
  me: () => unwrap<UserSummary>(generated.getCurrentUser()),
  categories: () => unwrap<CategoryResponse[]>(generated.getCategories()),
  products: (query: NonNullable<GetProductsData["query"]> = {}) => unwrap<PagedProductListItem>(generated.getProducts({ query })),
  product: (id: number) => unwrap<ProductDetail>(generated.getProduct({ path: { id } })),
  createProduct: (body: CreateProductRequest) => unwrap<ProductDetail>(generated.createProduct({ body })),
  updateProduct: (id: number, body: UpdateProductRequest) => unwrap<ProductDetail>(generated.updateProduct({ path: { id }, body })),
  reviewProduct: (id: number, body: ReviewProductRequest) => unwrap<ProductDetail>(generated.reviewProduct({ path: { id }, body })),
  productReviews: (id: number) => unwrap<ProductReviewResponse[]>(generated.getProductReviews({ path: { id } })),
  createReview: (id: number, body: CreateReviewRequest) => unwrap<ProductReviewResponse>(generated.createProductReview({ path: { id }, body })),
  cart: () => unwrap<CartItemResponse[]>(generated.getCart()),
  addCartItem: (body: AddCartItemRequest) => unwrap<CartItemResponse>(generated.addCartItem({ body })),
  updateCartItem: (id: number, body: UpdateCartItemRequest) => unwrap<CartItemResponse>(generated.updateCartItem({ path: { cartItemId: id }, body })),
  deleteCartItem: (id: number) => unwrap<{ itemId: number }>(generated.deleteCartItem({ path: { cartItemId: id } })),
  createOrder: (body: CreateOrderRequest) => unwrap<OrderDetail>(generated.createOrder({ body })),
  orders: () => unwrap<OrderSummary[]>(generated.getMyOrders()),
  order: (id: number) => unwrap<OrderDetail>(generated.getOrder({ path: { id } })),
  payOrder: (id: number, body: PayOrderRequest) => unwrap<OrderDetail>(generated.payOrder({ path: { id }, body })),
  shipOrder: (id: number) => unwrap<OrderDetail>(generated.shipOrder({ path: { id } })),
  completeOrder: (id: number) => unwrap<OrderDetail>(generated.completeOrder({ path: { id } })),
  cancelOrder: (id: number) => unwrap<OrderDetail>(generated.cancelOrder({ path: { id } })),
  applyMerchant: (body: ApplyMerchantRequest) => unwrap<MerchantSummary>(generated.applyMerchant({ body })),
  pendingMerchants: () => unwrap<MerchantSummary[]>(generated.getPendingMerchants()),
  reviewMerchant: (id: number, body: ReviewMerchantRequest) => unwrap<MerchantSummary>(generated.reviewMerchant({ path: { id }, body })),
  myMerchant: () => unwrap<MerchantSummary>(generated.getMyMerchant()),
  merchantProducts: () => unwrap<ProductListItem[]>(generated.getMerchantProducts()),
  merchantOrders: () => unwrap<OrderSummary[]>(generated.getMerchantOrders()),
  createTicket: (body: CreateTicketRequest) => unwrap<TicketResponse>(generated.createTicket({ body })),
  myTickets: () => unwrap<TicketResponse[]>(generated.getMyTickets()),
  assignedTickets: () => unwrap<TicketResponse[]>(generated.getAssignedTickets()),
  replyTicket: (id: number, body: ReplyTicketRequest) => unwrap<TicketResponse>(generated.replyTicket({ path: { id }, body })),
  overview: () => unwrap<OverviewReport>(generated.getOverviewReport()),
  dailySales: () => unwrap<DailySalesPoint[]>(generated.getDailySalesReport()),
  categorySales: () => unwrap<CategorySalesPoint[]>(generated.getCategorySalesReport()),
  merchantReport: () => unwrap<MerchantReport>(generated.getMerchantReport()),
  adminUsers: () => unwrap<UserSummary[]>(generated.getAdminUsers()),
  adminOrders: () => unwrap<OrderSummary[]>(generated.getAdminOrders()),
  pendingProducts: () => unwrap<ProductListItem[]>(generated.getPendingProducts()),
  allTickets: () => unwrap<TicketResponse[]>(generated.getAllTickets()),
}
