"use client"
import { create } from "zustand"
import { api } from "../lib/api/sdk" // 确保你已经把 lib/api 拷过来了

export const useAuthStore = create<any>((set) => ({
  user: null,
  hydrated: false,
  // 增加登录逻辑：包含本地存储
  login: async (username: string, password: string) => {
    const result = await api.login({ username, password })
    localStorage.setItem("retail-access-token", result.accessToken)
    localStorage.setItem("retail-user", JSON.stringify(result.user))
    set({ user: result.user })
    return result.user
  },
  logout: () => {
    localStorage.removeItem("retail-access-token")
    localStorage.removeItem("retail-user")
    set({ user: null })
  },
  // 增加数据恢复逻辑：防止刷新页面后登录状态丢失
  hydrate: () => {
    const raw = localStorage.getItem("retail-user")
    if (raw) {
      try { set({ user: JSON.parse(raw), hydrated: true }) }
      catch { set({ hydrated: true }) }
    } else {
      set({ hydrated: true })
    }
  }
}))