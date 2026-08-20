"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import type { z } from "zod";
import { Plus, Trash2, FilePlus2 } from "lucide-react";
import { useWorkflowDefinitions, useCreateRequest } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { newRequestSchema, type NewRequestInput } from "@/lib/validation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PageHeader } from "@/components/page-header";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface PayloadField {
  key: string;
  value: string;
}

export default function NewRequestPage() {
  const router = useRouter();
  const { data: workflows, isLoading: workflowsLoading } = useWorkflowDefinitions();
  const createRequest = useCreateRequest();

  const [fields, setFields] = useState<PayloadField[]>([{ key: "Department", value: "" }]);

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<z.input<typeof newRequestSchema>, unknown, NewRequestInput>({
    resolver: zodResolver(newRequestSchema),
    defaultValues: { workflowDefinitionId: "", title: "", amount: 0, currency: "USD", description: "" },
  });

  const activeWorkflows = workflows?.filter((w) => w.isActive) ?? [];

  function updateField(index: number, patch: Partial<PayloadField>) {
    setFields((prev) => prev.map((f, i) => (i === index ? { ...f, ...patch } : f)));
  }

  function addField() {
    setFields((prev) => [...prev, { key: "", value: "" }]);
  }

  function removeField(index: number) {
    setFields((prev) => prev.filter((_, i) => i !== index));
  }

  async function onSubmit(values: NewRequestInput) {
    const payload: Record<string, string> = {};
    if (values.description) payload.Description = values.description;
    for (const f of fields) {
      if (f.key.trim()) payload[f.key.trim()] = f.value;
    }

    try {
      const result = await createRequest.mutateAsync({
        workflowDefinitionId: values.workflowDefinitionId,
        title: values.title,
        amount: values.amount,
        currency: values.currency.toUpperCase(),
        payloadJson: JSON.stringify(payload),
      });
      toast.success("Solicitud enviada.");
      router.push(`/requests/${result.id}`);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear la solicitud.");
    }
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader
        icon={FilePlus2}
        title="Nueva solicitud"
        description="Se envía de inmediato: el motor de reglas calcula el primer paso automáticamente."
      />

      <Card className="glass rounded-2xl border-border/60">
        <CardHeader>
          <CardTitle className="text-base">Detalles</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="workflow">Flujo de aprobación</Label>
              <Controller
                control={control}
                name="workflowDefinitionId"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="workflow" className="w-full" aria-invalid={!!errors.workflowDefinitionId}>
                      <SelectValue placeholder={workflowsLoading ? "Cargando…" : "Selecciona un flujo"} />
                    </SelectTrigger>
                    <SelectContent>
                      {activeWorkflows.map((w) => (
                        <SelectItem key={w.id} value={w.id}>
                          {w.name} ({w.stepCount} paso{w.stepCount === 1 ? "" : "s"})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.workflowDefinitionId && (
                <p className="text-xs text-destructive">{errors.workflowDefinitionId.message}</p>
              )}
              {!workflowsLoading && activeWorkflows.length === 0 && (
                <p className="text-xs text-amber-600 dark:text-amber-400">
                  No hay flujos activos. Pídele a un administrador que active uno en &ldquo;Flujos&rdquo;.
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="title">Título</Label>
              <Input id="title" aria-invalid={!!errors.title} {...register("title")} />
              {errors.title && <p className="text-xs text-destructive">{errors.title.message}</p>}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="amount">Monto</Label>
                <Input
                  id="amount"
                  type="number"
                  min={0}
                  step="0.01"
                  aria-invalid={!!errors.amount}
                  {...register("amount")}
                />
                {errors.amount && <p className="text-xs text-destructive">{errors.amount.message}</p>}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="currency">Moneda</Label>
                <Input
                  id="currency"
                  maxLength={3}
                  className="uppercase"
                  aria-invalid={!!errors.currency}
                  {...register("currency")}
                />
                {errors.currency && <p className="text-xs text-destructive">{errors.currency.message}</p>}
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="description">Descripción</Label>
              <Textarea id="description" {...register("description")} />
              {errors.description && <p className="text-xs text-destructive">{errors.description.message}</p>}
            </div>

            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <Label>Campos adicionales</Label>
                <Button type="button" variant="ghost" size="sm" onClick={addField}>
                  <Plus className="size-3.5" />
                  Agregar campo
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">
                Usa estos campos para que el motor de reglas evalúe condiciones (ej. Department = Finance).
              </p>
              {fields.map((f, i) => (
                <div key={i} className="flex items-center gap-2">
                  <Input
                    placeholder="Campo (ej. Department)"
                    value={f.key}
                    onChange={(e) => updateField(i, { key: e.target.value })}
                  />
                  <Input
                    placeholder="Valor"
                    value={f.value}
                    onChange={(e) => updateField(i, { value: e.target.value })}
                  />
                  <Button type="button" variant="ghost" size="icon" onClick={() => removeField(i)}>
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              ))}
            </div>

            <Button type="submit" disabled={createRequest.isPending} className="shadow-glow mt-2 h-10">
              {createRequest.isPending ? "Enviando…" : "Enviar solicitud"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
