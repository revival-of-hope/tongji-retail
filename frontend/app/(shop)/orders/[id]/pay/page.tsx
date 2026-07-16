"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import { CreditCard } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { api } from "@/lib/api/sdk"
import type { OrderDetail, PaymentMethod } from "@/lib/api/generated/types.gen"
import { currency, dateTime } from "@/lib/format"

const methods: { value: PaymentMethod; label: string }[] = [
  { value: "Alipay", label: "支付宝" },
  { value: "WeChat", label: "微信支付" },
  { value: "CreditCard", label: "信用卡" },
]

export default function PayPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const id = Number(params.id)
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [method, setMethod] = useState<PaymentMethod>("Alipay")
  const [paying, setPaying] = useState(false)

  useEffect(() => { void api.order(id).then(setOrder).catch((error) => toast.error(error.message)) }, [id])

  const pay = async () => {
    setPaying(true)
    try {
      await api.payOrder(id, { paymentMethod: method })
      toast.success("支付成功，库存已扣减")
      router.replace("/orders")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "支付失败")
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
            <CardHeader><CardTitle className="flex items-center gap-2"><CreditCard />模拟支付</CardTitle><CardDescription>本课程项目不连接真实支付网关。</CardDescription></CardHeader>
            <CardContent className="space-y-6">
              <div className="rounded-lg bg-muted p-4"><p className="text-sm text-muted-foreground">订单号</p><p className="font-medium">{order?.orderNo ?? id}</p>{order && <p className="mt-2 text-xs text-muted-foreground">支付截止：{dateTime(order.expireAt)}</p>}</div>
              <div className="text-center text-4xl font-bold text-primary">{order ? currency(order.totalAmount) : "--"}</div>
              <RadioGroup value={method} onValueChange={(value) => setMethod(value as PaymentMethod)} className="space-y-3">{methods.map((item) => <Label key={item.value} htmlFor={item.value} className="flex cursor-pointer items-center gap-3 rounded-lg border p-4"><RadioGroupItem id={item.value} value={item.value} />{item.label}</Label>)}</RadioGroup>
              <Button className="w-full" disabled={paying || !order || order.status !== "PendingPayment"} onClick={() => void pay()}>{paying ? "支付处理中…" : "确认支付"}</Button>
            </CardContent>
          </Card>
        </main>
      </div>
    </AuthGuard>
  )
}
