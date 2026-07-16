"use client"

import Link from "next/link"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { UserPlus } from "lucide-react"
import { toast } from "sonner"
import { Navbar } from "@/components/layout/navbar"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuthStore } from "@/store/auth"

export default function RegisterPage() {
  const router = useRouter()
  const register = useAuthStore((state) => state.register)
  const [form, setForm] = useState({ username: "", email: "", password: "", confirm: "" })
  const [loading, setLoading] = useState(false)
  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (form.password !== form.confirm) { toast.error("两次密码输入不一致"); return }
    setLoading(true)
    try {
      await register(form.username, form.password, form.email)
      toast.success("注册成功")
      router.replace("/")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "注册失败")
    } finally {
      setLoading(false)
    }
  }
  return <div className="min-h-screen bg-muted/20"><Navbar /><main className="mx-auto max-w-md px-4 py-12"><Card><CardHeader><CardTitle className="flex items-center gap-2"><UserPlus />注册顾客账号</CardTitle><CardDescription>商家身份需要注册后另行提交入驻申请。</CardDescription></CardHeader><form onSubmit={submit}><CardContent className="space-y-4"><div className="space-y-2"><Label htmlFor="username">用户名</Label><Input id="username" value={form.username} onChange={(event) => setForm({ ...form, username: event.target.value })} required minLength={3} maxLength={50} autoComplete="username" /></div><div className="space-y-2"><Label htmlFor="email">邮箱</Label><Input id="email" type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} autoComplete="email" /></div><div className="space-y-2"><Label htmlFor="password">密码</Label><Input id="password" type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} required minLength={6} autoComplete="new-password" /></div><div className="space-y-2"><Label htmlFor="confirm">确认密码</Label><Input id="confirm" type="password" value={form.confirm} onChange={(event) => setForm({ ...form, confirm: event.target.value })} required autoComplete="new-password" /></div></CardContent><CardFooter className="flex-col gap-4"><Button className="w-full" disabled={loading}>{loading ? "注册中…" : "注册"}</Button><p className="text-sm text-muted-foreground">已有账号？<Link className="text-primary hover:underline" href="/auth/login">登录</Link></p></CardFooter></form></Card></main></div>
}
