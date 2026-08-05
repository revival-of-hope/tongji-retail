import { describe, expect, it } from "vitest"
import { currency, orderStatusLabels, roleLabels, ticketStatusLabels } from "@/lib/format"

describe("format helpers", () => {
  it("formats CNY amounts", () => {
    expect(currency(399)).toContain("399.00")
  })

  it("maps API enums to Chinese labels", () => {
    expect(roleLabels.Merchant).toBe("商家")
    expect(orderStatusLabels.PendingShipment).toBe("待发货")
    expect(ticketStatusLabels.Resolved).toBe("已解决")
  })
})
