"use client";

import { useState } from "react";
import Link from "next/link";
import { useRequests } from "@/lib/hooks";
import { formatCurrency, formatDateTime, statusLabel } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { EmptyState } from "@/components/empty-state";
import { PageHeader } from "@/components/page-header";
import type { RequestStatus } from "@/lib/types";
import { FileText, Plus } from "lucide-react";

const STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Draft: "outline",
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
  Cancelled: "outline",
};

const FILTERS: { value: RequestStatus | "all"; label: string }[] = [
  { value: "all", label: "Todas" },
  { value: "Pending", label: "Pendientes" },
  { value: "Approved", label: "Aprobadas" },
  { value: "Rejected", label: "Rechazadas" },
  { value: "Cancelled", label: "Canceladas" },
];

export default function RequestsPage() {
  const [filter, setFilter] = useState<RequestStatus | "all">("all");
  const { data, isLoading } = useRequests(filter === "all" ? undefined : filter);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon={FileText}
        title="Solicitudes"
        description="Historial de tus solicitudes de aprobación."
        action={
          <Button asChild className="shadow-glow h-9">
            <Link href="/requests/new">
              <Plus className="size-4" />
              Nueva solicitud
            </Link>
          </Button>
        }
      />

      <Tabs value={filter} onValueChange={(v) => setFilter(v as RequestStatus | "all")}>
        <TabsList>
          {FILTERS.map((f) => (
            <TabsTrigger key={f.value} value={f.value}>
              {f.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {isLoading && <Skeleton className="h-64 w-full rounded-2xl" />}

      {!isLoading && (
        <Card className="glass overflow-hidden rounded-2xl border-border/60 p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead>Título</TableHead>
                <TableHead>Monto</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Paso actual</TableHead>
                <TableHead>Creada</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data?.map((r) => (
                <TableRow key={r.id} className="group cursor-pointer transition-colors hover:bg-accent/60">
                  <TableCell className="font-medium">
                    <Link href={`/requests/${r.id}`} className="group-hover:text-primary">
                      {r.title}
                    </Link>
                  </TableCell>
                  <TableCell className="tabular-nums">{formatCurrency(r.amount, r.currency)}</TableCell>
                  <TableCell>
                    <Badge variant={STATUS_VARIANT[r.status] ?? "outline"}>{statusLabel(r.status)}</Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground">{r.currentStepName ?? "—"}</TableCell>
                  <TableCell className="text-muted-foreground">{formatDateTime(r.createdAt)}</TableCell>
                </TableRow>
              ))}
              {data?.length === 0 && (
                <TableRow className="hover:bg-transparent">
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
        </Card>
      )}
    </div>
  );
}
