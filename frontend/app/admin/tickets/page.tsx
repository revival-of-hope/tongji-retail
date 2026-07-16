"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { TicketResponse } from "@/lib/api/generated/types.gen"
import { dateTime, ticketStatusLabels } from "@/lib/format"

export default function AdminTicketsPage() {
  const [tickets, setTickets] = useState<TicketResponse[]>([])
  useEffect(() => { void api.allTickets().then(setTickets).catch((error) => toast.error(error.message)) }, [])
  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">客服工单</h1><p className="mt-1 text-sm text-muted-foreground">查看全平台工单分配与处理结果。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>主题</TableHead><TableHead>提交用户</TableHead><TableHead>关联订单</TableHead><TableHead>处理客服</TableHead><TableHead>状态</TableHead><TableHead>更新时间</TableHead></TableRow></TableHeader><TableBody>{tickets.map((ticket) => <TableRow key={ticket.id}><TableCell className="max-w-80 whitespace-normal"><p className="font-medium">{ticket.subject}</p><p className="line-clamp-2 text-xs text-muted-foreground">{ticket.description}</p></TableCell><TableCell>{ticket.username}</TableCell><TableCell>{ticket.orderId ?? "—"}</TableCell><TableCell>{ticket.assignedUsername ?? "未分配"}</TableCell><TableCell><Badge variant="secondary">{ticketStatusLabels[ticket.status]}</Badge></TableCell><TableCell>{dateTime(ticket.updatedAt)}</TableCell></TableRow>)}{tickets.length === 0 && <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">暂无工单</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
