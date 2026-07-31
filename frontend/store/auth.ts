"use client"

import { create } from "zustand"
import type { UserSummary } from "../lib/api/generated/types.gen"
import { api } from "../lib/api/sdk"

/**
 * 身份验证状态管理仓库 (Auth Store)
 * 负责全局用户状态、登录、注册及持久化逻辑
 */
type AuthState = {
  user: UserSummary | null;    // 当前登录用户信息
  hydrated: boolean;           // 标记本地存储数据是否已恢复
  login: (username: string, password: string) => Promise<UserSummary>;
  register: (username: string, password: string, email?: string) => Promise<UserSummary>;
  refresh: () => Promise<void>; // 刷新当前用户信息
  logout: () => void;           // 注销登录
  hydrate: () => void;          // 从 localStorage 初始化状态
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  hydrated: false,

  // 用户登录逻辑
  async login(username, password) {
    const result = await api.login({ username, password })
    // 持久化存储 Token 和用户信息，防止刷新页面丢失状态
    localStorage.setItem("retail-access-token", result.accessToken)
    localStorage.setItem("retail-user", JSON.stringify(result.user))
    set({ user: result.user })
    return result.user
  },

  // 用户注册逻辑
  async register(username, password, email) {
    const result = await api.register({ username, password, email: email || null, phone: null })
    localStorage.setItem("retail-access-token", result.accessToken)
    localStorage.setItem("retail-user", JSON.stringify(result.user))
    set({ user: result.user })
    return result.user
  },

  // 刷新逻辑：通过现有的 Token 向后端请求最新的用户信息
  async refresh() {
    const token = localStorage.getItem("retail-access-token")
    if (!token) return
    try {
      const user = await api.me()
      localStorage.setItem("retail-user", JSON.stringify(user))
      set({ user })
    } catch {
      // 若 Token 校验失败（过期等），则清除本地无效数据
      localStorage.removeItem("retail-access-token")
      localStorage.removeItem("retail-user")
      set({ user: null })
    }
  },

  // 注销：清除所有状态和本地缓存
  logout() {
    localStorage.removeItem("retail-access-token")
    localStorage.removeItem("retail-user")
    set({ user: null })
  },

  // 初始化：应用启动时从浏览器缓存中读取用户信息
  hydrate() {
    const raw = localStorage.getItem("retail-user")
    try {
      set({ user: raw ? (JSON.parse(raw) as UserSummary) : null, hydrated: true })
    } catch {
      localStorage.removeItem("retail-user")
      set({ user: null, hydrated: true })
    }
  },
}))