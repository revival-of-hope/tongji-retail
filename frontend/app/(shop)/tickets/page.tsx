"use client"

import { useEffect, useState } from "react"
import { Headphones } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { OrderSummary, TicketResponse } from "@/lib/api/generated/types.gen"
import { dateTime, ticketStatusLabels } from "@/lib/format"

export default function TicketsPage() {
  const [tickets, setTickets] = useState<TicketResponse[]>([])
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ orderId: "", subject: "", description: "" })

  const load = async () => {
    try {
      const [ticketData, orderData] = await Promise.all([api.myTickets(), api.orders()])
      setTickets(ticketData)
      setOrders(orderData)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "工单加载失败")
    }
  }
  useEffect(() => { void load() }, [])

  const submit = async () => {
    try {
      await api.createTicket({ orderId: form.orderId ? Number(form.orderId) : null, subject: form.subject.trim(), description: form.description.trim() })
      toast.success("工单已提交")
      setForm({ orderId: "", subject: "", description: "" })
      setOpen(false)
      await load()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "提交失败")
    }
  }

  return <AuthGuard roles={["Customer"]}><div className="min-h-screen bg-muted/20"><Navbar /><main className="mx-auto max-w-5xl space-y-6 px-4 py-8"><div className="flex flex-wrap items-center justify-between gap-3"><div><h1 className="text-2xl font-semibold">客服工单</h1><p className="mt-1 text-sm text-muted-foreground">工单会自动分配给当前负载较低的客服。</p></div><Dialog open={open} onOpenChange={setOpen}><DialogTrigger asChild><Button><Headphones />提交工单</Button></DialogTrigger><DialogContent><DialogHeader><DialogTitle>新建客服工单</DialogTitle><DialogDescription>可选关联本人订单，便于客服定位问题。</DialogDescription></DialogHeader><div className="space-y-4"><div className="space-y-2"><Label htmlFor="ticket-order">关联订单 ID（选填）</Label><Input id="ticket-order" inputMode="numeric" list="order-ids" value={form.orderId} onChange={(event) => setForm({ ...form, orderId: event.target.value })} /><datalist id="order-ids">{orders.map((order) => <option key={order.id} value={order.id}>{order.orderNo}</option>)}</datalist></div><div className="space-y-2"><Label htmlFor="ticket-subject">主题</Label><Input id="ticket-subject" value={form.subject} onChange={(event) => setForm({ ...form, subject: event.target.value })} maxLength={200} /></div><div className="space-y-2"><Label htmlFor="ticket-description">问题描述</Label><Textarea id="ticket-description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={2000} rows={6} /></div></div><DialogFooter><Button disabled={!form.subject.trim() || !form.description.trim()} onClick={() => void submit()}>提交</Button></DialogFooter></DialogContent></Dialog></div>{tickets.length === 0 && <Card><CardContent className="py-16 text-center text-muted-foreground">暂无工单</CardContent></Card>}{tickets.map((ticket) => <Card key={ticket.id}><CardHeader className="flex-row flex-wrap items-start justify-between gap-3"><div><CardTitle className="text-lg">{ticket.subject}</CardTitle><CardDescription>{dateTime(ticket.createdAt)}{ticket.orderId ? ` · 订单 ID ${ticket.orderId}` : ""}</CardDescription></div><Badge variant={ticket.status === "Closed" ? "outline" : "secondary"}>{ticketStatusLabels[ticket.status]}</Badge></CardHeader><CardContent className="space-y-4"><div><p className="text-xs font-medium text-muted-foreground">问题描述</p><p className="mt-1 whitespace-pre-wrap text-sm">{ticket.description}</p></div><div className="rounded-lg bg-muted p-4"><p className="text-xs font-medium text-muted-foreground">客服回复</p><p className="mt-1 whitespace-pre-wrap text-sm">{ticket.reply || "等待客服处理"}</p>{ticket.assignedUsername && <p className="mt-2 text-xs text-muted-foreground">处理客服：{ticket.assignedUsername}</p>}</div></CardContent></Card>)}</main></div></AuthGuard>
}
