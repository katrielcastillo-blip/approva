"use client";

import Link from "next/link";
import { toast } from "sonner";
import { useWorkflowDefinitions, useSetWorkflowDefinitionActive } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Plus } from "lucide-react";

export default function WorkflowsPage() {
  const { data, isLoading } = useWorkflowDefinitions();
  const setActive = useSetWorkflowDefinitionActive();

  async function toggle(id: string, isActive: boolean) {
    try {
      await setActive.mutateAsync({ id, isActive: !isActive });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar el flujo.");
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Flujos de aprobación</h1>
          <p className="text-sm text-muted-foreground">
            Cambia el comportamiento del sistema editando estas reglas — sin recompilar.
          </p>
        </div>
        <Button asChild>
          <Link href="/workflows/new">
            <Plus className="size-4" />
            Nuevo flujo
          </Link>
        </Button>
      </div>

      {isLoading && <Skeleton className="h-48 w-full" />}

      <div className="flex flex-col gap-3">
        {data?.map((w) => (
          <Card key={w.id}>
            <CardContent className="flex items-center justify-between py-4">
              <div>
                <div className="flex items-center gap-2">
                  <p className="font-medium">{w.name}</p>
                  <Badge variant={w.isActive ? "default" : "outline"}>
                    {w.isActive ? "Activo" : "Inactivo"}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground">
                  {w.entityType} · {w.stepCount} paso{w.stepCount === 1 ? "" : "s"} · v{w.version}
                </p>
              </div>
              <Button variant="outline" size="sm" onClick={() => toggle(w.id, w.isActive)}>
                {w.isActive ? "Desactivar" : "Activar"}
              </Button>
            </CardContent>
          </Card>
        ))}
        {data?.length === 0 && (
          <Card>
            <CardContent className="py-12 text-center text-muted-foreground">
              No hay flujos definidos todavía.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
