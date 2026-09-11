import type { Metadata } from "next"
import "./globals.css"
import { Providers } from "@/components/providers"

<<<<<<< HEAD
import { AuthGuard } from "@/components/auth/auth-guard"

const robotoHeading = Roboto({subsets:['latin'],variable:'--font-heading'});
=======
export const metadata: Metadata = {
  title: "同济商品零售管理系统",
  description: "Next.js 16 + ASP.NET Core 9 + EF Core 9 + Oracle 21c 商品零售管理系统",
}
>>>>>>> upstream/main

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
<<<<<<< HEAD
    <html
      lang="en"
      suppressHydrationWarning
      className={cn("antialiased", fontMono.variable, "font-sans", inter.variable, robotoHeading.variable)}
    >
      <body>
        <ThemeProvider>
          <AuthGuard>
            {children}
          </AuthGuard>
        </ThemeProvider>
=======
    <html lang="zh-CN" suppressHydrationWarning>
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <Providers>{children}</Providers>
>>>>>>> upstream/main
      </body>
    </html>
  )
}
