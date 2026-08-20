"use client";

import Link from "next/link";
import { useRequests } from "@/lib/hooks";
import { formatCurrency, formatDateTime, statusLabel } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { Plus } from "lucide-react";

const STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Draft: "outline",
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
  Cancelled: "outline",
};

export default function RequestsPage() {
  const { data, isLoading } = useRequests();

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Solicitudes</h1>
          <p className="text-sm text-muted-foreground">Historial de tus solicitudes de aprobación.</p>
        </div>
        <Button asChild>
          <Link href="/requests/new">
            <Plus className="size-4" />
            Nueva solicitud
          </Link>
        </Button>
      </div>

      {isLoading && <Skeleton className="h-64 w-full" />}

      {!isLoading && (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Título</TableHead>
              <TableHead>Monto</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Paso actual</TableHead>
              <TableHead>Creada</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.map((r) => (
              <TableRow key={r.id} className="cursor-pointer">
                <TableCell className="font-medium">
                  <Link href={`/requests/${r.id}`} className="hover:underline">
                    {r.title}
                  </Link>
                </TableCell>
                <TableCell>{formatCurrency(r.amount, r.currency)}</TableCell>
                <TableCell>
                  <Badge variant={STATUS_VARIANT[r.status] ?? "outline"}>{statusLabel(r.status)}</Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">{r.currentStepName ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground">{formatDateTime(r.createdAt)}</TableCell>
              </TableRow>
            ))}
            {data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>
                  <EmptyState
                    title="Todavía no has creado ninguna solicitud"
                    description="Tus solicitudes de aprobación van a aparecer acá."
                  />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
