import { beforeEach, describe, expect, it, vi } from "vitest"

const { loginMock } = vi.hoisted(() => ({ loginMock: vi.fn() }))
vi.mock("@/lib/api/sdk", () => ({
  api: {
    login: loginMock,
    register: vi.fn(),
    me: vi.fn(),
  },
}))

import { useAuthStore } from "@/store/auth"

const user = {
  id: 1,
  username: "customer",
  email: "customer@retail.local",
  phone: null,
  role: "Customer" as const,
  isActive: true,
  createdAt: "2026-07-14T00:00:00Z",
  merchant: null,
}

describe("auth store", () => {
  beforeEach(() => {
    useAuthStore.setState({ user: null, hydrated: false })
    loginMock.mockReset()
  })

  it("persists generated API login response", async () => {
    loginMock.mockResolvedValue({ accessToken: "token-123", expiresAt: "2026-07-15T00:00:00Z", user })
    const result = await useAuthStore.getState().login("customer", "Customer123!")
    expect(result).toEqual(user)
    expect(window.localStorage.getItem("retail-access-token")).toBe("token-123")
    expect(useAuthStore.getState().user?.username).toBe("customer")
  })

  it("hydrates cached user state", () => {
    window.localStorage.setItem("retail-user", JSON.stringify(user))
    useAuthStore.getState().hydrate()
    expect(useAuthStore.getState().hydrated).toBe(true)
    expect(useAuthStore.getState().user).toEqual(user)
  })
})
