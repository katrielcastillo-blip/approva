"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Inbox,
  FileText,
  Workflow,
  Users,
  BarChart3,
  LogOut,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { Button } from "@/components/ui/button";
import { OutOfOfficeToggle } from "@/components/out-of-office-toggle";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/approvals", label: "Aprobaciones", icon: Inbox },
  { href: "/requests", label: "Mis solicitudes", icon: FileText },
  { href: "/workflows", label: "Flujos", icon: Workflow, adminOnly: true },
  { href: "/users", label: "Usuarios", icon: Users, adminOnly: true },
  { href: "/analytics", label: "Analítica", icon: BarChart3 },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-60 shrink-0 flex-col border-r bg-muted/20">
        <div className="flex h-14 items-center border-b px-4">
          <span className="text-lg font-bold">Approva</span>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3">
          {navItems
            .filter((item) => !item.adminOnly || user?.role === "Admin")
            .map((item) => {
              const Icon = item.icon;
              const isActive = pathname.startsWith(item.href);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-muted hover:text-foreground"
                  )}
                >
                  <Icon className="size-4" />
                  {item.label}
                </Link>
              );
            })}
        </nav>
        <div className="border-t p-3">
          <div className="mb-2 px-1">
            <p className="truncate text-sm font-medium">{user?.name}</p>
            <p className="truncate text-xs text-muted-foreground">
              {user?.email} · {user?.role}
            </p>
          </div>
          <div className="mb-1">
            <OutOfOfficeToggle />
          </div>
          <Button variant="ghost" size="sm" className="w-full justify-start gap-2" onClick={logout}>
            <LogOut className="size-4" />
            Cerrar sesión
          </Button>
        </div>
      </aside>
      <main className="flex-1 overflow-x-hidden">
        <div className="mx-auto max-w-6xl p-6">{children}</div>
      </main>
    </div>
  );
}
