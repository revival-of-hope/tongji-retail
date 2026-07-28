import Link from "next/link"

type RoutePlaceholderProps = {
  title: string
  description: string
  detail?: string
}

export function RoutePlaceholder({
  title,
  description,
  detail,
}: RoutePlaceholderProps) {
  return (
    <main className="mx-auto flex min-h-[70vh] w-full max-w-4xl items-center px-6 py-16">
      <section className="w-full rounded-2xl border bg-background p-8 shadow-sm">
        <p className="text-sm text-muted-foreground">App Router 页面骨架</p>
        <h1 className="mt-2 text-3xl font-semibold">{title}</h1>
        <p className="mt-3 max-w-2xl text-muted-foreground">{description}</p>
        {detail ? (
          <p className="mt-4 rounded-lg bg-muted px-4 py-3 font-mono text-sm">
            {detail}
          </p>
        ) : null}
        <Link
          href="/"
          className="mt-8 inline-flex text-sm font-medium underline underline-offset-4"
        >
          返回商城首页
        </Link>
      </section>
    </main>
  )
}
