"use client"

import Link from "next/link"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { LogIn } from "lucide-react"
import { toast } from "sonner"
import { Navbar } from "@/components/layout/navbar"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuthStore } from "@/store/auth"

const roleHome = { Admin: "/admin", Merchant: "/merchant", CustomerService: "/cs", Customer: "/" } as const
const demos = [
  ["admin", "Admin123!", "管理员"],
  ["merchant", "Merchant123!", "商家"],
  ["customer", "Customer123!", "顾客"],
  ["service", "Service123!", "客服"],
] as const

export default function LoginPage() {
  const router = useRouter()
  const login = useAuthStore((state) => state.login)
  const [username, setUsername] = useState("")
  const [password, setPassword] = useState("")
  const [loading, setLoading] = useState(false)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoading(true)
    try {
      const user = await login(username, password)
      toast.success("登录成功")
      router.replace(roleHome[user.role])
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "登录失败")
    } finally {
      setLoading(false)
    }
  }

  return <div className="min-h-screen bg-muted/20"><Navbar /><main className="mx-auto max-w-md px-4 py-12"><Card><CardHeader><CardTitle className="flex items-center gap-2"><LogIn />登录</CardTitle><CardDescription>使用账号密码进入对应功能界面。</CardDescription></CardHeader><form onSubmit={submit}><CardContent className="space-y-5"><div className="space-y-2"><Label htmlFor="username">用户名</Label><Input id="username" value={username} onChange={(event) => setUsername(event.target.value)} autoComplete="username" required /></div><div className="space-y-2"><Label htmlFor="password">密码</Label><Input id="password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" required /></div><div className="rounded-lg border bg-muted/40 p-3"><p className="mb-2 text-xs font-medium">演示账号</p><div className="grid grid-cols-2 gap-2">{demos.map(([name, secret, role]) => <Button key={name} type="button" variant="outline" size="sm" onClick={() => { setUsername(name); setPassword(secret) }}>{role}</Button>)}</div></div></CardContent><CardFooter className="flex-col gap-4"><Button className="w-full" disabled={loading}>{loading ? "登录中…" : "登录"}</Button><p className="text-sm text-muted-foreground">没有账号？<Link className="text-primary hover:underline" href="/auth/register">注册顾客账号</Link></p></CardFooter></form></Card></main></div>
}
