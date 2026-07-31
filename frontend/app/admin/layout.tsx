import { SectionLayout } from "@/app/_components/section-layout"

export default function AdminLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return <SectionLayout title="管理员后台">{children}</SectionLayout>
}
