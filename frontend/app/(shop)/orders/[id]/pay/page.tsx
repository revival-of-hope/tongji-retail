"use client"

import { useEffect, useMemo, useRef, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import { CreditCard } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { api } from "@/lib/api/sdk"
import type { OrderDetail, PaymentMethod } from "@/lib/api/generated/types.gen"
import { currency, dateTime, orderStatusLabels } from "@/lib/format"

const methods: { value: PaymentMethod; label: string }[] = [
  { value: "Alipay", label: "支付宝" },
  { value: "WeChat", label: "微信支付" },
  { value: "CreditCard", label: "信用卡" },
]

function formatRemaining(milliseconds: number) {
  const totalSeconds = Math.max(0, Math.floor(milliseconds / 1000))
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`
}

export default function PayPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const id = Number(params.id)
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [method, setMethod] = useState<PaymentMethod>("Alipay")
  const [paying, setPaying] = useState(false)
  const [now, setNow] = useState(Date.now())
  const refreshedAfterExpiry = useRef(false)

  useEffect(() => {
    if (!Number.isSafeInteger(id) || id <= 0) {
      toast.error("订单编号无效")
      router.replace("/orders")
      return
    }

    void api
      .order(id)
      .then(setOrder)
      .catch((error) => toast.error(error instanceof Error ? error.message : "订单加载失败"))
  }, [id, router])

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])

  const remaining = useMemo(
    () => (order ? new Date(order.expireAt).getTime() - now : 0),
    [now, order],
  )
  const expired = Boolean(order && order.status === "PendingPayment" && remaining <= 0)

  useEffect(() => {
    if (!expired || refreshedAfterExpiry.current) return
    refreshedAfterExpiry.current = true
    void api
      .order(id)
      .then(setOrder)
      .catch(() => undefined)
  }, [expired, id])

  const pay = async () => {
    if (!order || expired) return
    setPaying(true)
    try {
      await api.payOrder(id, { paymentMethod: method })
      toast.success("支付成功，库存已扣减")
      router.replace("/orders")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "支付失败")
      try {
        setOrder(await api.order(id))
      } catch {
        // Keep the original error visible when refreshing the order also fails.
      }
    } finally {
      setPaying(false)
    }
  }

  return (
    <AuthGuard roles={["Customer"]}>
      <div className="min-h-screen bg-muted/20">
        <Navbar />
        <main className="mx-auto max-w-lg px-4 py-12">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CreditCard />
                模拟支付
              </CardTitle>
              <CardDescription>本课程项目不连接真实支付网关。</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="rounded-lg bg-muted p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm text-muted-foreground">订单号</p>
                    <p className="font-medium">{order?.orderNo ?? id}</p>
                  </div>
                  {order && <Badge variant="secondary">{orderStatusLabels[order.status]}</Badge>}
                </div>
                {order && (
                  <div className="mt-3 space-y-1 text-xs text-muted-foreground">
                    <p>支付截止：{dateTime(order.expireAt)}</p>
                    {order.status === "PendingPayment" && (
                      <p className={expired ? "font-medium text-destructive" : "font-medium text-foreground"}>
                        {expired ? "支付期限已结束" : `剩余时间：${formatRemaining(remaining)}`}
                      </p>
                    )}
                  </div>
                )}
              </div>

              <div className="text-center text-4xl font-bold text-primary">
                {order ? currency(order.totalAmount) : "--"}
              </div>

              <RadioGroup
                value={method}
                onValueChange={(value) => setMethod(value as PaymentMethod)}
                className="space-y-3"
              >
                {methods.map((item) => (
                  <Label
                    key={item.value}
                    htmlFor={item.value}
                    className="flex cursor-pointer items-center gap-3 rounded-lg border p-4"
                  >
                    <RadioGroupItem id={item.value} value={item.value} />
                    {item.label}
                  </Label>
                ))}
              </RadioGroup>

              <Button
                className="w-full"
                disabled={paying || !order || order.status !== "PendingPayment" || expired}
                onClick={() => void pay()}
              >
                {paying ? "支付处理中…" : expired ? "订单已过期" : "确认支付"}
              </Button>
            </CardContent>
          </Card>
        </main>
      </div>
    </AuthGuard>
  )
}
