"use client"

import { useState } from "react"
import Link from "next/link"
import {
  ArrowUpRight,
  Barcode,
  Boxes,
  CheckCircle2,
  ChevronLeft,
  PackageSearch,
  Plus,
  RefreshCw,
  Search,
  ShoppingCart,
  TrendingUp,
  AlertTriangle,
} from "lucide-react"
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
import { Input } from "@/components/ui/input"

interface StockItem {
  id: string
  name: string
  sku: string
  price: number
  stock: number
  status: "abundant" | "warning" | "out"
}

const initialStocks: StockItem[] = [
  { id: "1", name: "特仑苏纯牛奶 250ml", sku: "690123456", price: 68.0, stock: 3, status: "warning" },
  { id: "2", name: "无公害红富士苹果 (1kg)", sku: "690789012", price: 15.8, stock: 0, status: "out" },
  { id: "3", name: "天然弱碱性矿泉水 550ml", sku: "690234567", price: 2.5, stock: 128, status: "abundant" },
]

export default function RetailDashboardDemo() {
  const [stocks, setStocks] = useState<StockItem[]>(initialStocks)
  const [searchSku, setSearchSku] = useState("")
  const [adjustSku, setAdjustSku] = useState("")
  const [adjustPrice, setAdjustPrice] = useState("")
  const [adjustStock, setAdjustStock] = useState("")
  const [isScanning, setIsScanning] = useState(false)

  // 快捷改价 / 调库提交
  const handleUpdateStock = (e: React.FormEvent) => {
    e.preventDefault()
    const targetSku = adjustSku.trim()
    if (!targetSku) {
      toast.warning("请输入商品条码或 SKU")
      return
    }

    const matched = stocks.some(
      (item) => item.sku === targetSku || item.name.toLowerCase().includes(targetSku.toLowerCase())
    )

    if (!matched) {
      toast.error(`未找到商品 [${targetSku}]，请核对条码或商品名称`)
      return
    }

    const added = parseInt(adjustStock, 10) || 0
    const newPriceParsed = adjustPrice ? parseFloat(adjustPrice) : undefined

    setStocks((prev) =>
      prev.map((item) => {
        if (item.sku === targetSku || item.name.toLowerCase().includes(targetSku.toLowerCase())) {
          const newStock = Math.max(0, item.stock + added)
          const newPrice = newPriceParsed !== undefined && !isNaN(newPriceParsed) ? Math.max(0, newPriceParsed) : item.price
          return {
            ...item,
            price: newPrice,
            stock: newStock,
            status: newStock === 0 ? "out" : newStock < 10 ? "warning" : "abundant",
          }
        }
        return item
      })
    )
    toast.success(`商品 [${targetSku}] 属性已成功更新！`)
    setAdjustSku("")
    setAdjustPrice("")
    setAdjustStock("")
  }

  // 模拟摄像头扫码
  const handleScanBarcode = () => {
    setIsScanning(true)
    setTimeout(() => {
      setIsScanning(false)
      const randomItem = stocks[Math.floor(Math.random() * stocks.length)]
      if (randomItem) {
        setAdjustSku(randomItem.sku)
        setAdjustPrice(randomItem.price.toString())
        setAdjustStock("10")
        toast.success(`扫描成功：识别到商品 [${randomItem.name}] (SKU: ${randomItem.sku})`)
      }
    }, 1200)
  }

  // 过滤后的库存监控列表
  const filteredStocks = stocks.filter(
    (item) =>
      item.name.toLowerCase().includes(searchSku.toLowerCase()) ||
      item.sku.includes(searchSku)
  )

  return (
    <div className="min-h-screen bg-muted/20 text-foreground">
      {/* 1. 顶部 Header */}
      <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6">
          <div className="flex items-center gap-6">
            <Link
              href="/"
              className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              <ChevronLeft className="size-4" /> 返回主商城
            </Link>
            <div className="flex items-center gap-2 font-bold text-base tracking-tight">
              <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                <ShoppingCart className="size-4" />
              </div>
              <span>零售管理工作台 (Frontend02 Demo)</span>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="relative hidden sm:block w-64">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <Input
                value={searchSku}
                onChange={(e) => setSearchSku(e.target.value)}
                className="pl-8 h-9 text-xs"
                placeholder="搜索监控商品、SKU..."
              />
            </div>
            <Button size="sm" onClick={() => toast.info("演示功能：已打开新增商品弹窗")}>
              <Plus className="mr-1 size-4" /> 新增商品
            </Button>
          </div>
        </div>
      </header>

      {/* 2. 主内容区 */}
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 space-y-8">
        {/* 顶部指标卡片 (Grid 布局) */}
        <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Card className="shadow-sm border-border transition hover:shadow-md">
            <CardHeader className="pb-2 flex flex-row items-center justify-between space-y-0">
              <CardDescription className="text-xs font-medium">今日营业总额</CardDescription>
              <div className="p-2 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400">
                <TrendingUp className="size-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tracking-tight">￥12,850.00</div>
              <p className="mt-1 flex items-center text-xs text-emerald-600 font-medium">
                <ArrowUpRight className="mr-1 size-3.5" /> 较昨日增长 12.5%
              </p>
            </CardContent>
          </Card>

          <Card className="shadow-sm border-border transition hover:shadow-md">
            <CardHeader className="pb-2 flex flex-row items-center justify-between space-y-0">
              <CardDescription className="text-xs font-medium">今日成交订单</CardDescription>
              <div className="p-2 rounded-lg bg-primary/10 text-primary">
                <CheckCircle2 className="size-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tracking-tight">142 单</div>
              <p className="mt-1 text-xs text-muted-foreground">平均客单价：￥90.50</p>
            </CardContent>
          </Card>

          <Card className="shadow-sm border-border transition hover:shadow-md sm:col-span-2 lg:col-span-1">
            <CardHeader className="pb-2 flex flex-row items-center justify-between space-y-0">
              <CardDescription className="text-xs font-medium">异常库存预警</CardDescription>
              <div className="p-2 rounded-lg bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400">
                <AlertTriangle className="size-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold tracking-tight text-destructive">
                {stocks.filter((s) => s.status !== "abundant").length} 件商品
              </div>
              <p className="mt-1 text-xs text-rose-500 font-medium">包含售罄与临界低库存</p>
            </CardContent>
          </Card>
        </section>

        {/* 核心操作与监控区 (4 列 Grid) */}
        <section className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
          {/* 卡片 1: 实时库存监控 */}
          <Card className="shadow-sm lg:col-span-2">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="space-y-1">
                  <CardTitle className="text-base font-semibold flex items-center gap-2">
                    <Boxes className="size-4 text-primary" /> 库存状态监控
                  </CardTitle>
                  <CardDescription className="text-xs">实时显示当前店铺关键库存</CardDescription>
                </div>
                <Badge variant="outline" className="text-xs">
                  共 {filteredStocks.length} 条记录
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              {filteredStocks.map((item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between p-3 rounded-lg border bg-card/50 hover:bg-card transition-colors"
                >
                  <div className="space-y-0.5">
                    <p className="text-xs font-semibold">{item.name}</p>
                    <div className="flex items-center gap-2 text-[11px] text-muted-foreground font-mono">
                      <span>SKU: {item.sku}</span>
                      <span>·</span>
                      <span className="font-semibold text-foreground">￥{item.price.toFixed(2)}</span>
                    </div>
                  </div>
                  <div>
                    {item.status === "out" ? (
                      <Badge variant="destructive">已售罄</Badge>
                    ) : item.status === "warning" ? (
                      <Badge className="bg-amber-500 hover:bg-amber-600 text-white">
                        仅剩 {item.stock} 件
                      </Badge>
                    ) : (
                      <Badge className="bg-emerald-600 hover:bg-emerald-700 text-white">
                        库存充足 ({item.stock})
                      </Badge>
                    )}
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* 卡片 2: 商品快速改价与调库 */}
          <Card className="shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-base font-semibold flex items-center gap-2">
                <RefreshCw className="size-4 text-primary" /> 快速改价/调库
              </CardTitle>
              <CardDescription className="text-xs">无需进详情页直接调整属性</CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleUpdateStock} className="space-y-3">
                <div className="space-y-1">
                  <label className="text-[11px] font-medium text-muted-foreground">
                    商品条码 / SKU
                  </label>
                  <Input
                    value={adjustSku}
                    onChange={(e) => setAdjustSku(e.target.value)}
                    placeholder="输入或扫码填入 SKU"
                    className="h-8 text-xs"
                  />
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div className="space-y-1">
                    <label className="text-[11px] font-medium text-muted-foreground">
                      调整单价 (元)
                    </label>
                    <Input
                      type="number"
                      value={adjustPrice}
                      onChange={(e) => setAdjustPrice(e.target.value)}
                      placeholder="18.50"
                      className="h-8 text-xs"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[11px] font-medium text-muted-foreground">
                      增补库存
                    </label>
                    <Input
                      type="number"
                      value={adjustStock}
                      onChange={(e) => setAdjustStock(e.target.value)}
                      placeholder="20"
                      className="h-8 text-xs"
                    />
                  </div>
                </div>
                <Button type="submit" size="sm" className="w-full h-8 text-xs mt-2">
                  确认更新属性
                </Button>
              </form>
            </CardContent>
          </Card>

          {/* 卡片 3: 条码录入 / 智能收银 */}
          <Card className="shadow-sm flex flex-col justify-between p-6 text-center">
            <div className="space-y-3 flex flex-col items-center">
              <div className="grid size-20 place-items-center rounded-2xl border-2 border-dashed border-primary/40 bg-primary/5">
                <Barcode className="size-10 text-primary" />
              </div>
              <div className="space-y-1">
                <p className="text-sm font-semibold">扫码录入 / 移动收银</p>
                <p className="text-xs text-muted-foreground leading-relaxed">
                  支持外接 USB 扫码枪，或点击下方模拟调用摄像头快速读取商品条码。
                </p>
              </div>
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={isScanning}
              onClick={handleScanBarcode}
              className="mt-4 w-full text-xs"
            >
              {isScanning ? (
                <>
                  <RefreshCw className="mr-1.5 size-3.5 animate-spin" /> 识别中...
                </>
              ) : (
                <>
                  <PackageSearch className="mr-1.5 size-3.5" /> 模拟扫描条形码
                </>
              )}
            </Button>
          </Card>
        </section>

        {/* 底部业务说明 */}
        <footer className="text-center text-xs text-muted-foreground py-4 border-t">
          同济大学零售电商管理系统 · frontend02 单页应用实战 Demo
        </footer>
      </main>
    </div>
  )
}
