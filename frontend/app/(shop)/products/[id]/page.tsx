import { RoutePlaceholder } from "@/app/_components/route-placeholder"

type ProductPageProps = {
  params: Promise<{ id: string }>
}

export default async function ProductPage({ params }: ProductPageProps) {
  const { id } = await params

  return (
    <RoutePlaceholder
      title="商品详情"
      description="展示商品图片、价格、库存、评价并提供加入购物车操作。"
      detail={`商品 ID：${id}`}
    />
  )
}
