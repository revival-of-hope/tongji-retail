import { SectionLayout } from "@/app/_components/section-layout"

export default function MerchantLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return <SectionLayout title="商家中心">{children}</SectionLayout>
}
