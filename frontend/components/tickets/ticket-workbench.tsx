"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { MessageSquareReply } from "lucide-react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Textarea } from "@/components/ui/textarea"
import { api } from "@/lib/api/sdk"
import type { TicketResponse, TicketStatus } from "@/lib/api/generated/types.gen"
import { dateTime, ticketStatusLabels } from "@/lib/format"

type TicketMode = "admin" | "service"
type TicketFilter = "all" | TicketStatus

const statusOptions: { value: TicketStatus; label: string }[] = [
  { value: "Processing", label: "处理中" },
  { value: "Resolved", label: "已解决" },
  { value: "Closed", label: "已关闭" },
]

const filterOptions: { value: TicketFilter; label: string }[] = [
  { value: "all", label: "全部" },
  { value: "Pending", label: "待处理" },
  { value: "Processing", label: "处理中" },
  { value: "Resolved", label: "已解决" },
  { value: "Closed", label: "已关闭" },
]

export function TicketWorkbench({ mode }: { mode: TicketMode }) {
  const [tickets, setTickets] = useState<TicketResponse[]>([])
  const [filter, setFilter] = useState<TicketFilter>("all")
  const [target, setTarget] = useState<TicketResponse | null>(null)
  const [reply, setReply] = useState("")
  const [status, setStatus] = useState<TicketStatus>("Processing")
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    try {
      setTickets(mode === "admin" ? await api.allTickets() : await api.assignedTickets())
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "工单加载失败")
    }
  }, [mode])

  useEffect(() => {
    void load()
  }, [load])

  const visibleTickets = useMemo(
    () => (filter === "all" ? tickets : tickets.filter((ticket) => ticket.status === filter)),
    [filter, tickets],
  )

  const openReply = (ticket: TicketResponse) => {
    setTarget(ticket)
    setReply(ticket.reply ?? "")
    setStatus(ticket.status === "Pending" ? "Processing" : ticket.status)
  }

  const submit = async () => {
    if (!target || !reply.trim()) return
    setSaving(true)
    try {
      await api.replyTicket(target.id, { reply: reply.trim(), status })
      toast.success("工单已更新")
      setTarget(null)
      await load()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "工单更新失败")
    } finally {
      setSaving(false)
    }
  }

  const title = mode === "admin" ? "客服工单" : "客服工作台"
  const description =
    mode === "admin"
      ? "查看并处理全平台工单；管理员可回复任何工单并更新处理状态。"
      : "显示分配给当前客服及尚未分配的工单；回复未分配工单时自动接单。"

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{description}</p>
      </div>

      <Tabs value={filter} onValueChange={(value) => setFilter(value as TicketFilter)}>
        <TabsList className="flex h-auto flex-wrap">
          {filterOptions.map((option) => (
            <TabsTrigger key={option.value} value={option.value}>
              {option.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <div className="grid gap-4 lg:grid-cols-2">
        {visibleTickets.map((ticket) => (
          <Card key={ticket.id}>
            <CardHeader>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <CardTitle className="text-lg">{ticket.subject}</CardTitle>
                  <CardDescription>
                    {ticket.username} · {dateTime(ticket.createdAt)}
                    {ticket.orderId ? ` · 订单 ${ticket.orderId}` : ""}
                  </CardDescription>
                </div>
                <Badge variant="secondary">{ticketStatusLabels[ticket.status]}</Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <p className="text-xs font-medium text-muted-foreground">用户描述</p>
                <p className="mt-1 whitespace-pre-wrap text-sm">{ticket.description}</p>
              </div>
              <div className="grid gap-3 rounded-lg bg-muted p-3 text-sm sm:grid-cols-2">
                <div>
                  <p className="text-xs text-muted-foreground">处理人</p>
                  <p>{ticket.assignedUsername ?? "尚未分配"}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">最后更新</p>
                  <p>{dateTime(ticket.updatedAt)}</p>
                </div>
              </div>
              {ticket.reply && (
                <div className="rounded-lg border p-3">
                  <p className="text-xs font-medium text-muted-foreground">当前回复</p>
                  <p className="mt-1 whitespace-pre-wrap text-sm">{ticket.reply}</p>
                </div>
              )}
              <Button variant="outline" onClick={() => openReply(ticket)}>
                <MessageSquareReply />
                回复与更新状态
              </Button>
            </CardContent>
          </Card>
        ))}

        {visibleTickets.length === 0 && (
          <Card className="lg:col-span-2">
            <CardContent className="py-16 text-center text-muted-foreground">
              没有符合条件的工单
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog open={target !== null} onOpenChange={(open) => !open && setTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>回复工单</DialogTitle>
            <DialogDescription>{target?.subject}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="ticket-reply">回复内容</Label>
              <Textarea
                id="ticket-reply"
                value={reply}
                onChange={(event) => setReply(event.target.value)}
                rows={7}
                maxLength={2000}
              />
              <p className="text-right text-xs text-muted-foreground">{reply.length}/2000</p>
            </div>
            <div className="space-y-2">
              <Label>处理状态</Label>
              <Select value={status} onValueChange={(value) => setStatus(value as TicketStatus)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {statusOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button disabled={saving || !reply.trim()} onClick={() => void submit()}>
              {saving ? "保存中…" : "保存回复"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
