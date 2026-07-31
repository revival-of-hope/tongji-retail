/**
 * 路由守卫组件
 * 逻辑：
 * 1. 页面加载时先尝试从 localStorage 恢复登录状态 (hydrate)
 * 2. 如果数据加载完成 (hydrated) 且用户未登录，则重定向至登录页
 * 3. 排除 auth 相关的路径，避免死循环
 */
"use client"
import { useAuthStore } from "@/store/auth"
import { useRouter, usePathname } from "next/navigation"
import { useEffect } from "react"

export function AuthGuard({ children }: { children: React.ReactNode }) {
  const { user, hydrated, hydrate } = useAuthStore()
  const router = useRouter()
  const pathname = usePathname()

  useEffect(() => { hydrate() }, [hydrate])

  useEffect(() => {
    if (hydrated && !user && !pathname.startsWith('/auth')) {
      router.push('/auth/login')
    }
  }, [user, hydrated, pathname, router])

  return <>{children}</>
}
