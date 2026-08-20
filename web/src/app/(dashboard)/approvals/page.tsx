"use client";

import Link from "next/link";
import { usePendingApprovals } from "@/lib/hooks";
import { formatCurrency, formatRelativeToNow } from "@/lib/format";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";

export default function ApprovalsPage() {
  const { data, isLoading } = usePendingApprovals();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Aprobaciones pendientes</h1>
        <p className="text-sm text-muted-foreground">Tareas asignadas a ti que esperan una decisión.</p>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
      )}

      {!isLoading && data?.length === 0 && (
        <Card>
          <CardContent>
            <EmptyState title="No tienes aprobaciones pendientes" description="Estás al día. 🎉" />
          </CardContent>
        </Card>
      )}

      <div className="flex flex-col gap-3">
        {data?.map((item) => (
          <Link key={item.taskId} href={`/requests/${item.requestId}`}>
            <Card className="transition-colors hover:border-primary">
              <CardContent className="flex items-center justify-between gap-4 py-4">
                <div className="flex flex-col gap-1">
                  <div className="flex items-center gap-2">
                    <p className="font-medium">{item.requestTitle}</p>
                    {item.isOverdue && <Badge variant="destructive">Vencida</Badge>}
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {item.requesterName} · Paso: {item.stepName}
                  </p>
                </div>
                <div className="flex flex-col items-end gap-1">
                  <span className="font-semibold">{formatCurrency(item.amount, item.currency)}</span>
                  <span className="text-xs text-muted-foreground">
                    Vence {formatRelativeToNow(item.dueAt)}
                  </span>
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
