"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import type { UserRole } from "@/lib/types";

export function AuthGuard({
  children,
  requireRole,
}: {
  children: ReactNode;
  requireRole?: UserRole;
}) {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;
    if (!user) {
      router.replace("/login");
      return;
    }
    if (requireRole && user.role !== requireRole) {
      router.replace("/");
    }
  }, [isLoading, user, requireRole, router]);

  if (isLoading || !user || (requireRole && user.role !== requireRole)) {
    return (
      <div className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">
        Cargando…
      </div>
    );
  }

  return <>{children}</>;
}
