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
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { OutOfOfficeToggle } from "@/components/out-of-office-toggle";
import { ThemeToggle } from "@/components/theme-toggle";
import { Logo } from "@/components/logo";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/approvals", label: "Aprobaciones", icon: Inbox },
  { href: "/requests", label: "Mis solicitudes", icon: FileText },
  { href: "/workflows", label: "Flujos", icon: Workflow, adminOnly: true },
  { href: "/users", label: "Usuarios", icon: Users, adminOnly: true },
  { href: "/analytics", label: "Analítica", icon: BarChart3 },
];

function initials(name?: string) {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase();
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  return (
    <div className="bg-mesh-subtle flex min-h-screen">
      <aside className="glass sticky top-0 flex h-screen w-64 shrink-0 flex-col">
        <div className="flex h-16 items-center gap-2 border-b border-border/60 px-5">
          <Logo className="size-7 rounded-md" />
          <span className="text-lg font-bold tracking-tight">Approva</span>
        </div>

        <nav className="flex flex-1 flex-col gap-1 overflow-y-auto p-3">
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
                    "group relative flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200",
                    isActive
                      ? "shadow-glow bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:translate-x-0.5 hover:bg-accent hover:text-accent-foreground"
                  )}
                >
                  <Icon className={cn("size-4 shrink-0 transition-transform", !isActive && "group-hover:scale-110")} />
                  {item.label}
                </Link>
              );
            })}
        </nav>

        <div className="border-t border-border/60 p-3">
          <div className="mb-2 flex items-center gap-2.5 rounded-xl px-1.5 py-1.5">
            <Avatar className="size-8">
              <AvatarFallback className="bg-primary/15 text-xs font-semibold text-primary">
                {initials(user?.name)}
              </AvatarFallback>
            </Avatar>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium">{user?.name}</p>
              <p className="truncate text-xs text-muted-foreground">{user?.role}</p>
            </div>
            <ThemeToggle />
          </div>
          <div className="mb-1">
            <OutOfOfficeToggle />
          </div>
          <Button variant="ghost" size="sm" className="w-full justify-start gap-2 text-muted-foreground" onClick={logout}>
            <LogOut className="size-4" />
            Cerrar sesión
          </Button>
        </div>
      </aside>
      <main className="flex-1 overflow-x-hidden">
        <div className="mx-auto max-w-6xl p-6 lg:p-8">{children}</div>
      </main>
    </div>
  );
}
