import { existsSync } from "node:fs"
import { join } from "node:path"
import { describe, expect, it } from "vitest"

const appDir = join(process.cwd(), "app")

describe("merchant route structure", () => {
  it("keeps the merchant dashboard and customer application in one route tree", () => {
    expect(existsSync(join(appDir, "merchant", "layout.tsx"))).toBe(true)
    expect(existsSync(join(appDir, "merchant", "page.tsx"))).toBe(true)
    expect(existsSync(join(appDir, "merchant", "products", "page.tsx"))).toBe(true)
    expect(existsSync(join(appDir, "merchant", "orders", "page.tsx"))).toBe(true)
    expect(existsSync(join(appDir, "merchant", "apply", "page.tsx"))).toBe(true)

    expect(existsSync(join(appDir, "(merchant-dashboard)"))).toBe(false)
    expect(existsSync(join(appDir, "(shop)", "merchant"))).toBe(false)
  })
})
