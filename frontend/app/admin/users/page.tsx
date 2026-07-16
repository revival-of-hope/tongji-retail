"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { UserSummary } from "@/lib/api/generated/types.gen"
import { dateTime, roleLabels } from "@/lib/format"

export default function AdminUsersPage() {
  const [users, setUsers] = useState<UserSummary[]>([])
  useEffect(() => { void api.adminUsers().then(setUsers).catch((error) => toast.error(error.message)) }, [])
  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">用户管理</h1><p className="mt-1 text-sm text-muted-foreground">当前接口提供只读用户总览，账号状态由数据库管理员维护。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>用户</TableHead><TableHead>角色</TableHead><TableHead>联系方式</TableHead><TableHead>状态</TableHead><TableHead>注册时间</TableHead></TableRow></TableHeader><TableBody>{users.map((user) => <TableRow key={user.id}><TableCell><p className="font-medium">{user.username}</p><p className="text-xs text-muted-foreground">ID {user.id}</p></TableCell><TableCell>{roleLabels[user.role]}</TableCell><TableCell><p>{user.email || "—"}</p><p className="text-xs text-muted-foreground">{user.phone || ""}</p></TableCell><TableCell><Badge variant={user.isActive ? "secondary" : "destructive"}>{user.isActive ? "启用" : "停用"}</Badge></TableCell><TableCell>{dateTime(user.createdAt)}</TableCell></TableRow>)}{users.length === 0 && <TableRow><TableCell colSpan={5} className="py-12 text-center text-muted-foreground">暂无用户</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
