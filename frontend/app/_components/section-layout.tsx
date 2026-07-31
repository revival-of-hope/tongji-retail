import Link from "next/link"

type SectionLayoutProps = {
  title: string
  children: React.ReactNode
}

export function SectionLayout({ title, children }: SectionLayoutProps) {
  return (
    <div className="min-h-svh">
      <header className="border-b">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <span className="font-medium">{title}</span>
          <Link href="/" className="text-sm underline underline-offset-4">
            返回商城
          </Link>
        </div>
      </header>
      {children}
    </div>
  )
}
