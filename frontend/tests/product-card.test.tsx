import { render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { ProductCard } from "@/components/product/product-card"

const product = {
  id: 7,
  name: "测试机械键盘",
  price: 399,
  stockQuantity: 10,
  soldCount: 20,
  avgRating: 4.8,
  reviewCount: 9,
  status: "OnSale" as const,
  storeName: "测试店铺",
  categoryName: "数码",
  mainImageUrl: null,
  createdAt: "2026-07-14T00:00:00Z",
}

describe("ProductCard", () => {
  it("renders contract data and product link", () => {
    render(<ProductCard product={product} />)
    expect(screen.getByText("测试机械键盘")).toBeInTheDocument()
    expect(screen.getByText("测试店铺")).toBeInTheDocument()
    expect(screen.getByRole("link", { name: "查看测试机械键盘" })).toHaveAttribute("href", "/products/7")
  })
})
