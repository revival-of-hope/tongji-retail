"use client"

import { AuthGuard } from "@/components/auth/auth-guard"
import { Navbar } from "@/components/layout/navbar"
import { TicketWorkbench } from "@/components/tickets/ticket-workbench"

export default function CustomerServicePage() {
  return (
    <AuthGuard roles={["CustomerService"]}>
      <div className="min-h-screen bg-muted/20">
        <Navbar />
        <main className="mx-auto max-w-6xl px-4 py-8">
          <TicketWorkbench mode="service" />
        </main>
      </div>
    </AuthGuard>
  )
}
