"use client";

import Link from "next/link";
import { toast } from "sonner";
import { useWorkflowDefinitions, useSetWorkflowDefinitionActive } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { PageHeader } from "@/components/page-header";
import { Plus, Workflow, GitBranch } from "lucide-react";

export default function WorkflowsPage() {
  const { data, isLoading } = useWorkflowDefinitions();
  const setActive = useSetWorkflowDefinitionActive();

  async function toggle(id: string, isActive: boolean) {
    try {
      await setActive.mutateAsync({ id, isActive: !isActive });
      toast.success(isActive ? "Flujo desactivado." : "Flujo activado.");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar el flujo.");
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon={Workflow}
        title="Flujos de aprobación"
        description="Cambia el comportamiento del sistema editando estas reglas — sin recompilar."
        action={
          <Button asChild className="shadow-glow h-9">
            <Link href="/workflows/new">
              <Plus className="size-4" />
              Nuevo flujo
            </Link>
          </Button>
        }
      />

      {isLoading && (
        <div className="flex flex-col gap-3">
          {[1, 2].map((i) => (
            <Skeleton key={i} className="h-20 w-full rounded-2xl" />
          ))}
        </div>
      )}

      <div className="flex flex-col gap-3">
        {data?.map((w) => (
          <Card key={w.id} className="glass rounded-2xl border-border/60 py-0 transition-colors hover:border-primary/30">
            <CardContent className="flex items-center justify-between gap-4 px-5 py-4">
              <div className="flex items-center gap-3">
                <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                  <GitBranch className="size-5" />
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <p className="font-medium">{w.name}</p>
                    <Badge variant={w.isActive ? "default" : "outline"}>{w.isActive ? "Activo" : "Inactivo"}</Badge>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {w.entityType} · {w.stepCount} paso{w.stepCount === 1 ? "" : "s"} · v{w.version}
                  </p>
                </div>
              </div>
              <Button variant="outline" size="sm" onClick={() => toggle(w.id, w.isActive)}>
                {w.isActive ? "Desactivar" : "Activar"}
              </Button>
            </CardContent>
          </Card>
        ))}
        {data?.length === 0 && (
          <Card className="glass rounded-2xl border-dashed border-border/60">
            <CardContent>
              <EmptyState
                title="No hay flujos definidos todavía"
                description="Crea el primero para empezar a enrutar solicitudes automáticamente."
              />
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
