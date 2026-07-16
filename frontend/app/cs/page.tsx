"use client"

import { useEffect, useMemo, useState } from "react"
import { MessageSquareReply } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { TicketResponse, TicketStatus } from "@/lib/api/generated/types.gen"
import { dateTime, ticketStatusLabels } from "@/lib/format"

export default function CustomerServicePage() {
  const [tickets, setTickets] = useState<TicketResponse[]>([])
  const [filter, setFilter] = useState<"all" | TicketStatus>("all")
  const [target, setTarget] = useState<TicketResponse | null>(null)
  const [reply, setReply] = useState("")
  const [status, setStatus] = useState<TicketStatus>("Processing")

  const load = async () => { try { setTickets(await api.assignedTickets()) } catch (error) { toast.error(error instanceof Error ? error.message : "工单加载失败") } }
  useEffect(() => { void load() }, [])
  const visible = useMemo(() => filter === "all" ? tickets : tickets.filter((ticket) => ticket.status === filter), [filter, tickets])

  const openReply = (ticket: TicketResponse) => {
    setTarget(ticket)
    setReply(ticket.reply ?? "")
    setStatus(ticket.status === "Pending" ? "Processing" : ticket.status)
  }
  const submit = async () => {
    if (!target) return
    try {
      await api.replyTicket(target.id, { reply: reply.trim(), status })
      toast.success("工单已更新")
      setTarget(null)
      await load()
    } catch (error) { toast.error(error instanceof Error ? error.message : "回复失败") }
  }

  return <AuthGuard roles={["CustomerService"]}><div className="min-h-screen bg-muted/20"><Navbar /><main className="mx-auto max-w-6xl space-y-6 px-4 py-8"><div><h1 className="text-2xl font-semibold">客服工作台</h1><p className="mt-1 text-sm text-muted-foreground">显示分配给当前客服及尚未分配的工单；回复未分配工单时将自动由当前客服接单。</p></div><Tabs value={filter} onValueChange={(value) => setFilter(value as "all" | TicketStatus)}><TabsList className="flex h-auto flex-wrap"><TabsTrigger value="all">全部</TabsTrigger><TabsTrigger value="Processing">处理中</TabsTrigger><TabsTrigger value="Resolved">已解决</TabsTrigger><TabsTrigger value="Closed">已关闭</TabsTrigger></TabsList></Tabs><div className="grid gap-4 lg:grid-cols-2">{visible.map((ticket) => <Card key={ticket.id}><CardHeader><div className="flex items-start justify-between gap-3"><div><CardTitle className="text-lg">{ticket.subject}</CardTitle><CardDescription>{ticket.username} · {dateTime(ticket.createdAt)}{ticket.orderId ? ` · 订单 ${ticket.orderId}` : ""}</CardDescription></div><Badge variant="secondary">{ticketStatusLabels[ticket.status]}</Badge></div></CardHeader><CardContent className="space-y-4"><div><p className="text-xs font-medium text-muted-foreground">用户描述</p><p className="mt-1 whitespace-pre-wrap text-sm">{ticket.description}</p></div>{ticket.reply && <div className="rounded-lg bg-muted p-3"><p className="text-xs font-medium text-muted-foreground">当前回复</p><p className="mt-1 whitespace-pre-wrap text-sm">{ticket.reply}</p></div>}<Button variant="outline" onClick={() => openReply(ticket)}><MessageSquareReply />回复与更新状态</Button></CardContent></Card>)}{visible.length === 0 && <Card className="lg:col-span-2"><CardContent className="py-16 text-center text-muted-foreground">没有符合条件的工单</CardContent></Card>}</div></main><Dialog open={target !== null} onOpenChange={(open) => !open && setTarget(null)}><DialogContent><DialogHeader><DialogTitle>回复工单</DialogTitle><DialogDescription>{target?.subject}</DialogDescription></DialogHeader><div className="space-y-4"><div className="space-y-2"><Label htmlFor="reply">回复内容</Label><Textarea id="reply" value={reply} onChange={(event) => setReply(event.target.value)} rows={7} maxLength={2000} /></div><div className="space-y-2"><Label>处理状态</Label><Select value={status} onValueChange={(value) => setStatus(value as TicketStatus)}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent><SelectItem value="Processing">处理中</SelectItem><SelectItem value="Resolved">已解决</SelectItem><SelectItem value="Closed">已关闭</SelectItem></SelectContent></Select></div></div><DialogFooter><Button disabled={!reply.trim()} onClick={() => void submit()}>保存回复</Button></DialogFooter></DialogContent></Dialog></div></AuthGuard>
}
