"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Navbar } from "@/components/layout/navbar"

export type DashboardNavItem = { href: string; label: string }
export function DashboardShell({ title, items, children }: { title: string; items: DashboardNavItem[]; children: React.ReactNode }) {
  const pathname = usePathname()
  return <div className="min-h-screen bg-muted/30"><Navbar /><div className="mx-auto grid max-w-7xl gap-6 px-4 py-6 lg:grid-cols-[220px_1fr]"><Card className="h-fit"><CardHeader><CardTitle className="text-lg">{title}</CardTitle></CardHeader><CardContent className="flex gap-2 overflow-x-auto lg:flex-col">{items.map((item) => <Button key={item.href} variant={pathname === item.href ? "secondary" : "ghost"} className="justify-start" asChild><Link href={item.href}>{item.label}</Link></Button>)}</CardContent></Card><main className="min-w-0">{children}</main></div></div>
}
