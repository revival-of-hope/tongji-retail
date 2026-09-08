"use client"

import { useEffect, useState } from "react"
import { Store } from "lucide-react"
import { toast } from "sonner"
import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { ApiError, api } from "@/lib/api/sdk"
import type { MerchantSummary } from "@/lib/api/generated/types.gen"
import { merchantStatusLabels } from "@/lib/format"

export default function MerchantApplyPage() {
  const [merchant, setMerchant] = useState<MerchantSummary | null>(null)
  const [form, setForm] = useState({ storeName: "", description: "" })
  const [submitting, setSubmitting] = useState(false)

  const load = async () => {
    try { setMerchant(await api.myMerchant()) } catch (error) { if (!(error instanceof ApiError && error.status === 404)) console.error(error) }
  }
  useEffect(() => { void load() }, [])

  const submit = async () => {
    setSubmitting(true)
    try {
      const result = await api.applyMerchant({ storeName: form.storeName.trim(), description: form.description.trim() || null })
      setMerchant(result)
      toast.success("商家申请已提交")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "申请失败")
    } finally {
      setSubmitting(false)
    }
  }

  return <AuthGuard roles={["Customer"]}><div className="min-h-screen bg-muted/20"><Navbar /><main className="mx-auto max-w-2xl px-4 py-10"><Card><CardHeader><CardTitle className="flex items-center gap-2"><Store />商家入驻申请</CardTitle><CardDescription>审核通过后，当前账号将获得商家角色并进入商家工作台。</CardDescription></CardHeader><CardContent className="space-y-5">{merchant && <div className="rounded-lg border bg-muted/40 p-4"><div className="flex items-center justify-between gap-3"><strong>{merchant.storeName}</strong><Badge>{merchantStatusLabels[merchant.status]}</Badge></div><p className="mt-2 text-sm text-muted-foreground">{merchant.description || "未填写描述"}</p></div>}{(!merchant || merchant.status === "Rejected") && <><div className="space-y-2"><Label htmlFor="store-name">店铺名称</Label><Input id="store-name" value={form.storeName} onChange={(event) => setForm({ ...form, storeName: event.target.value })} maxLength={100} /></div><div className="space-y-2"><Label htmlFor="store-description">店铺描述</Label><Textarea id="store-description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} maxLength={500} rows={5} /></div><Button className="w-full" disabled={submitting || !form.storeName.trim()} onClick={() => void submit()}>{submitting ? "提交中…" : merchant?.status === "Rejected" ? "重新提交" : "提交申请"}</Button></>}</CardContent></Card></main></div></AuthGuard>
}
