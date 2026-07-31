import { beforeEach, describe, expect, it, vi } from "vitest"

// 手动模拟浏览器环境中的 window 和 localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value.toString() },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { store = {} }
  }
})()

// 模拟全局 window 对象
Object.defineProperty(global, 'window', {
  value: { localStorage: localStorageMock }
})
// 同时也模拟全局 localStorage 以防万一
Object.defineProperty(global, 'localStorage', {
  value: localStorageMock
})

// 原始测试逻辑

const { loginMock } = vi.hoisted(() => ({ loginMock: vi.fn() }))
vi.mock("../lib/api/sdk", () => ({
  api: {
    login: loginMock,
    register: vi.fn(),
    me: vi.fn(),
  },
}))

import { useAuthStore } from "../store/auth"

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

describe("Auth Store 状态流转测试", () => {
  beforeEach(() => {
    useAuthStore.setState({ user: null, hydrated: false })
    loginMock.mockReset()
    window.localStorage.clear()
  })

  it("登录成功后应该能正确持久化数据", async () => {
    loginMock.mockResolvedValue({ accessToken: "token-123", user })
    const result = await useAuthStore.getState().login("customer", "password")
    
    expect(result).toEqual(user)
    expect(window.localStorage.getItem("retail-access-token")).toBe("token-123")
    expect(useAuthStore.getState().user?.username).toBe("customer")
  })

  it("Hydrate 功能应该能从缓存恢复状态", () => {
    window.localStorage.setItem("retail-user", JSON.stringify(user))
    useAuthStore.getState().hydrate()
    expect(useAuthStore.getState().hydrated).toBe(true)
    expect(useAuthStore.getState().user).toEqual(user)
  })
})