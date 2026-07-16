"use client"

import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { LogOut, Menu, Moon, Store, Sun } from "lucide-react"
import { useTheme } from "next-themes"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { roleLabels } from "@/lib/format"
import { getNavigationItems, workspaceRoutes } from "@/lib/navigation"
import { useAuthStore } from "@/store/auth"

export function Navbar() {
  const pathname = usePathname()
  const router = useRouter()
  const { resolvedTheme, setTheme } = useTheme()
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)
  const links = getNavigationItems(user?.role)
  const workspace = user ? workspaceRoutes[user.role] : undefined
  const signOut = () => { logout(); router.push("/") }
  const navLinks = <>{links.map((link) => <Button key={link.href} variant={pathname === link.href ? "secondary" : "ghost"} asChild><Link href={link.href}>{link.label}</Link></Button>)}</>

  return (
    <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-4 px-4">
        <Link href="/" className="flex items-center gap-2 font-semibold"><Store className="size-5" />同济零售</Link>
        <nav className="hidden flex-1 items-center gap-1 md:flex">{navLinks}</nav>
        <div className="ml-auto flex items-center gap-2">
          <Button variant="ghost" size="icon" aria-label="切换主题" onClick={() => setTheme(resolvedTheme === "dark" ? "light" : "dark")}>{resolvedTheme === "dark" ? <Sun /> : <Moon />}</Button>
          {user ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild><Button variant="ghost" className="gap-2"><Avatar className="size-7"><AvatarFallback>{user.username.slice(0, 1).toUpperCase()}</AvatarFallback></Avatar><span className="hidden sm:inline">{user.username}</span></Button></DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>{roleLabels[user.role]}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {workspace ? <DropdownMenuItem onSelect={() => router.push(workspace)}>进入工作台</DropdownMenuItem> : null}
                <DropdownMenuItem onSelect={signOut}><LogOut />退出登录</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : <><Button variant="ghost" asChild><Link href="/auth/login">登录</Link></Button><Button asChild><Link href="/auth/register">注册</Link></Button></>}
          <Sheet>
            <SheetTrigger asChild><Button className="md:hidden" variant="outline" size="icon"><Menu /><span className="sr-only">打开导航</span></Button></SheetTrigger>
            <SheetContent><SheetHeader><SheetTitle>导航</SheetTitle></SheetHeader><nav className="mt-6 flex flex-col gap-2">{navLinks}{user ? <Button variant="destructive" onClick={signOut}><LogOut />退出登录</Button> : null}</nav></SheetContent>
          </Sheet>
        </div>
      </div>
    </header>
  )
}
