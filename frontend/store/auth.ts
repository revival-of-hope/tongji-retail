"use client"

import { create } from "zustand"
import type { UserSummary } from "@/lib/api/generated/types.gen"
import { api } from "@/lib/api/sdk"

type AuthState = {
  user: UserSummary | null
  hydrated: boolean
  login: (username: string, password: string) => Promise<UserSummary>
  register: (username: string, password: string, email?: string) => Promise<UserSummary>
  refresh: () => Promise<void>
  logout: () => void
  hydrate: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  hydrated: false,
  async login(username, password) {
    const result = await api.login({ username, password })
    localStorage.setItem("retail-access-token", result.accessToken)
    localStorage.setItem("retail-user", JSON.stringify(result.user))
    set({ user: result.user })
    return result.user
  },
  async register(username, password, email) {
    const result = await api.register({ username, password, email: email || null, phone: null })
    localStorage.setItem("retail-access-token", result.accessToken)
    localStorage.setItem("retail-user", JSON.stringify(result.user))
    set({ user: result.user })
    return result.user
  },
  async refresh() {
    const token = localStorage.getItem("retail-access-token")
    if (!token) return
    try {
      const user = await api.me()
      localStorage.setItem("retail-user", JSON.stringify(user))
      set({ user })
    } catch {
      localStorage.removeItem("retail-access-token")
      localStorage.removeItem("retail-user")
      set({ user: null })
    }
  },
  logout() {
    localStorage.removeItem("retail-access-token")
    localStorage.removeItem("retail-user")
    set({ user: null })
  },
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
