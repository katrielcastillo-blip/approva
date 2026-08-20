export function formatCurrency(amount: number, currency: string) {
  return new Intl.NumberFormat("es-ES", { style: "currency", currency }).format(amount);
}

export function formatDateTime(iso: string) {
  return new Intl.DateTimeFormat("es-ES", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(iso));
}

export function formatRelativeToNow(iso: string) {
  const diffMs = new Date(iso).getTime() - Date.now();
  const diffHours = diffMs / (1000 * 60 * 60);
  const rtf = new Intl.RelativeTimeFormat("es-ES", { numeric: "auto" });

  if (Math.abs(diffHours) < 1) {
    return rtf.format(Math.round(diffMs / (1000 * 60)), "minute");
  }
  if (Math.abs(diffHours) < 24) {
    return rtf.format(Math.round(diffHours), "hour");
  }
  return rtf.format(Math.round(diffHours / 24), "day");
}

const STATUS_LABELS: Record<string, string> = {
  Draft: "Borrador",
  Pending: "Pendiente",
  Approved: "Aprobada",
  Rejected: "Rechazada",
  Cancelled: "Cancelada",
};

export function statusLabel(status: string) {
  return STATUS_LABELS[status] ?? status;
}

const TASK_STATUS_LABELS: Record<string, string> = {
  Pending: "Pendiente",
  Approved: "Aprobada",
  Rejected: "Rechazada",
  Escalated: "Escalada",
  Delegated: "Delegada",
  Skipped: "Omitida",
};

export function taskStatusLabel(status: string) {
  return TASK_STATUS_LABELS[status] ?? status;
}

const ROLE_LABELS: Record<string, string> = {
  Requester: "Solicitante",
  Approver: "Aprobador",
  Admin: "Administrador",
};

export function roleLabel(role: string) {
  return ROLE_LABELS[role] ?? role;
}

const EVENT_LABELS: Record<string, string> = {
  RequestCreated: "Solicitud creada",
  RequestSubmitted: "Solicitud enviada",
  TaskAssigned: "Tarea asignada",
  TaskApproved: "Tarea aprobada",
  TaskRejected: "Tarea rechazada",
  TaskEscalated: "Tarea escalada",
  TaskDelegated: "Tarea delegada",
  RequestApproved: "Solicitud aprobada",
  RequestRejected: "Solicitud rechazada",
  RequestCancelled: "Solicitud cancelada",
};

export function eventLabel(eventType: string) {
  return EVENT_LABELS[eventType] ?? eventType;
}
