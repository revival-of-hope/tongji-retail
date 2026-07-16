"use client"

import { useEffect, useState } from "react"
import { Check, X } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { api } from "@/lib/api/sdk"
import type { MerchantSummary } from "@/lib/api/generated/types.gen"
import { dateTime } from "@/lib/format"

export default function AdminMerchantsPage() {
  const [items, setItems] = useState<MerchantSummary[]>([])
  const load = async () => { try { setItems(await api.pendingMerchants()) } catch (error) { toast.error(error instanceof Error ? error.message : "加载失败") } }
  useEffect(() => { void load() }, [])
  const review = async (id: number, approved: boolean) => { try { await api.reviewMerchant(id, { approved }); toast.success(approved ? "商家已通过" : "商家已拒绝"); await load() } catch (error) { toast.error(error instanceof Error ? error.message : "审核失败") } }
  return <div className="space-y-6"><div><h1 className="text-2xl font-semibold">商家审核</h1><p className="mt-1 text-sm text-muted-foreground">通过申请会把对应用户角色更新为商家。</p></div><Card><CardContent className="p-0"><Table><TableHeader><TableRow><TableHead>店铺</TableHead><TableHead>描述</TableHead><TableHead>用户 ID</TableHead><TableHead>提交时间</TableHead><TableHead className="text-right">审核</TableHead></TableRow></TableHeader><TableBody>{items.map((item) => <TableRow key={item.id}><TableCell className="font-medium">{item.storeName}</TableCell><TableCell className="max-w-80 whitespace-normal">{item.description || "—"}</TableCell><TableCell>{item.userId}</TableCell><TableCell>{dateTime(item.createdAt)}</TableCell><TableCell><div className="flex justify-end gap-2"><Button size="sm" onClick={() => void review(item.id, true)}><Check />通过</Button><Button size="sm" variant="destructive" onClick={() => void review(item.id, false)}><X />拒绝</Button></div></TableCell></TableRow>)}{items.length === 0 && <TableRow><TableCell colSpan={5} className="py-12 text-center text-muted-foreground">没有待审核申请</TableCell></TableRow>}</TableBody></Table></CardContent></Card></div>
}
