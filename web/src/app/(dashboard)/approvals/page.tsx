"use client";

import Link from "next/link";
import { usePendingApprovals } from "@/lib/hooks";
import { formatCurrency, formatRelativeToNow } from "@/lib/format";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { PageHeader } from "@/components/page-header";
import { Inbox, ChevronRight, Clock } from "lucide-react";

export default function ApprovalsPage() {
  const { data, isLoading } = usePendingApprovals();

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon={Inbox}
        title="Aprobaciones pendientes"
        description="Tareas asignadas a ti que esperan una decisión."
      />

      {isLoading && (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-24 w-full rounded-2xl" />
          ))}
        </div>
      )}

      {!isLoading && data?.length === 0 && (
        <Card className="glass border-dashed">
          <CardContent>
            <EmptyState title="No tienes aprobaciones pendientes" description="Estás al día. 🎉" />
          </CardContent>
        </Card>
      )}

      <div className="flex flex-col gap-3">
        {data?.map((item) => (
          <Link key={item.taskId} href={`/requests/${item.requestId}`} className="group">
            <Card className="glass shadow-glow overflow-hidden rounded-2xl border-border/60 py-0 transition-all duration-200 group-hover:-translate-y-0.5 group-hover:border-primary/40">
              <CardContent className="flex items-center justify-between gap-4 px-5 py-4">
                <div className="flex min-w-0 flex-col gap-1">
                  <div className="flex items-center gap-2">
                    <p className="truncate font-medium">{item.requestTitle}</p>
                    {item.isOverdue && (
                      <Badge variant="destructive" className="gap-1">
                        <Clock className="size-3" />
                        Vencida
                      </Badge>
                    )}
                  </div>
                  <p className="truncate text-sm text-muted-foreground">
                    {item.requesterName} · Paso: {item.stepName}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-3">
                  <div className="flex flex-col items-end gap-1">
                    <span className="font-semibold">{formatCurrency(item.amount, item.currency)}</span>
                    <span className="text-xs text-muted-foreground">Vence {formatRelativeToNow(item.dueAt)}</span>
                  </div>
                  <ChevronRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
