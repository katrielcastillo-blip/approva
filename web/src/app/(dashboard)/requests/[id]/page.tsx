"use client";

import { use, useState } from "react";
import { toast } from "sonner";
import { useRequest, useDecideRequest, useCancelRequest } from "@/lib/hooks";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api-client";
import { formatCurrency, formatDateTime, statusLabel, taskStatusLabel, eventLabel } from "@/lib/format";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { CheckCircle2, XCircle, Clock, User as UserIcon } from "lucide-react";

const STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Draft: "outline",
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
  Cancelled: "outline",
};

const TASK_STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
  Escalated: "outline",
  Delegated: "outline",
  Skipped: "outline",
};

export default function RequestDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { user } = useAuth();
  const { data: request, isLoading } = useRequest(id);
  const decide = useDecideRequest();
  const cancel = useCancelRequest();
  const [comment, setComment] = useState("");

  if (isLoading || !request) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-10 w-1/2" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  const myPendingTask = request.tasks.find((t) => t.status === "Pending" && t.assignedToUserId === user?.userId);
  const canCancel =
    (request.requesterId === user?.userId || user?.role === "Admin") &&
    (request.status === "Draft" || request.status === "Pending");

  async function handleDecision(decision: "Approve" | "Reject") {
    try {
      await decide.mutateAsync({ requestId: id, decision, comment: comment || undefined });
      toast.success(decision === "Approve" ? "Solicitud aprobada." : "Solicitud rechazada.");
      setComment("");
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error("Esta tarea ya fue decidida por otra persona. Recargando…");
      } else {
        toast.error(err instanceof ApiError ? err.message : "No se pudo procesar la decisión.");
      }
    }
  }

  async function handleCancel() {
    try {
      await cancel.mutateAsync(id);
      toast.success("Solicitud cancelada.");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cancelar.");
    }
  }

  let payload: Record<string, unknown> = {};
  try {
    payload = JSON.parse(request.payloadJson);
  } catch {
    // payload was empty or malformed — ignore, nothing to show
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold">{request.title}</h1>
            <Badge variant={STATUS_VARIANT[request.status] ?? "outline"}>{statusLabel(request.status)}</Badge>
          </div>
          <p className="text-sm text-muted-foreground">
            {request.requesterName} · {request.workflowDefinitionName} · {formatDateTime(request.createdAt)}
          </p>
        </div>
        <div className="text-right">
          <p className="text-2xl font-bold">{formatCurrency(request.amount, request.currency)}</p>
        </div>
      </div>

      {myPendingTask && (
        <Card className="border-primary">
          <CardHeader>
            <CardTitle className="text-base">Tienes una decisión pendiente</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Textarea
              placeholder="Comentario (opcional)"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
            />
            <div className="flex gap-2">
              <Button onClick={() => handleDecision("Approve")} disabled={decide.isPending}>
                <CheckCircle2 className="size-4" />
                Aprobar
              </Button>
              <Button variant="destructive" onClick={() => handleDecision("Reject")} disabled={decide.isPending}>
                <XCircle className="size-4" />
                Rechazar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {Object.keys(payload).length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Detalles</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
            {Object.entries(payload).map(([key, value]) => (
              <div key={key} className="flex justify-between border-b py-1">
                <span className="text-muted-foreground">{key}</span>
                <span className="font-medium">{String(value)}</span>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Tareas de aprobación</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {request.tasks.length === 0 && (
            <p className="text-sm text-muted-foreground">Sin pasos aplicables — aprobación automática.</p>
          )}
          {request.tasks.map((t) => (
            <div key={t.id} className="flex items-center justify-between rounded-md border p-3 text-sm">
              <div className="flex items-center gap-3">
                <UserIcon className="size-4 text-muted-foreground" />
                <div>
                  <p className="font-medium">{t.stepName}</p>
                  <p className="text-muted-foreground">{t.assignedToUserName}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                {t.status === "Pending" && (
                  <span className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Clock className="size-3" />
                    Vence {formatDateTime(t.dueAt)}
                  </span>
                )}
                <Badge variant={TASK_STATUS_VARIANT[t.status] ?? "outline"}>{taskStatusLabel(t.status)}</Badge>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Auditoría</CardTitle>
        </CardHeader>
        <CardContent>
          <ol className="flex flex-col gap-4">
            {request.auditTrail.map((event, i) => (
              <li key={event.id} className="relative flex gap-3 pl-4">
                <div className="absolute top-1.5 left-0 size-2 rounded-full bg-primary" />
                {i < request.auditTrail.length - 1 && (
                  <div className="absolute top-3.5 left-[3px] h-full w-px bg-border" />
                )}
                <div className="flex-1">
                  <p className="text-sm font-medium">{eventLabel(event.eventType)}</p>
                  <p className="text-xs text-muted-foreground">
                    {event.actorName} · {formatDateTime(event.occurredAt)}
                  </p>
                </div>
              </li>
            ))}
          </ol>
        </CardContent>
      </Card>

      {canCancel && (
        <>
          <Separator />
          <div>
            <Button variant="outline" onClick={handleCancel} disabled={cancel.isPending}>
              Cancelar solicitud
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
