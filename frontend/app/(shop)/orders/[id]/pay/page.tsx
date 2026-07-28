import { RoutePlaceholder } from "@/app/_components/route-placeholder"

type PayOrderPageProps = {
  params: Promise<{ id: string }>
}

export default async function PayOrderPage({ params }: PayOrderPageProps) {
  const { id } = await params

  return (
    <RoutePlaceholder
      title="订单支付"
      description="选择模拟支付方式并完成付款。"
      detail={`订单 ID：${id}`}
    />
  )
}
