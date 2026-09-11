<<<<<<< HEAD
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
=======
"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { Card, CardContent } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import type { UserRole } from "@/lib/api/generated/types.gen"
import { useAuthStore } from "@/store/auth"

export function AuthGuard({ roles, children }: { roles?: UserRole[]; children: React.ReactNode }) {
  const router = useRouter()
  const user = useAuthStore((state) => state.user)
  const hydrated = useAuthStore((state) => state.hydrated)

  useEffect(() => {
    if (!hydrated) return
    if (!user) router.replace("/auth/login")
    else if (roles && !roles.includes(user.role)) router.replace("/")
  }, [hydrated, roles, router, user])

  if (!hydrated || !user || (roles && !roles.includes(user.role))) {
    return <div className="mx-auto grid min-h-[60vh] max-w-md place-items-center px-4"><Card className="w-full"><CardContent className="space-y-3 p-6"><Skeleton className="h-5 w-1/3" /><Skeleton className="h-10 w-full" /><Skeleton className="h-10 w-2/3" /></CardContent></Card></div>
  }
  return children
>>>>>>> upstream/main
}
