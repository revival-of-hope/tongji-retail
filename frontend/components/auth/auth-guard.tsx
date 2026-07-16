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
}
