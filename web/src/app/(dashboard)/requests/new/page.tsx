"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Plus, Trash2 } from "lucide-react";
import { useWorkflowDefinitions, useCreateRequest } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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

  const [workflowId, setWorkflowId] = useState("");
  const [title, setTitle] = useState("");
  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState("USD");
  const [description, setDescription] = useState("");
  const [fields, setFields] = useState<PayloadField[]>([{ key: "Department", value: "" }]);

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

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const payload: Record<string, string> = {};
    if (description) payload.Description = description;
    for (const f of fields) {
      if (f.key.trim()) payload[f.key.trim()] = f.value;
    }

    try {
      const result = await createRequest.mutateAsync({
        workflowDefinitionId: workflowId,
        title,
        amount: Number(amount),
        currency,
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
      <div>
        <h1 className="text-2xl font-bold">Nueva solicitud</h1>
        <p className="text-sm text-muted-foreground">
          Se envía de inmediato: el motor de reglas calcula el primer paso automáticamente.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Detalles</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="workflow">Flujo de aprobación</Label>
              <Select value={workflowId} onValueChange={setWorkflowId} required>
                <SelectTrigger id="workflow" className="w-full">
                  <SelectValue placeholder={workflowsLoading ? "Cargando…" : "Selecciona un flujo"} />
                </SelectTrigger>
                <SelectContent>
                  {activeWorkflows.map((w) => (
                    <SelectItem key={w.id} value={w.id}>
                      {w.name} ({w.stepCount} pasos)
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="title">Título</Label>
              <Input id="title" value={title} onChange={(e) => setTitle(e.target.value)} required />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="amount">Monto</Label>
                <Input
                  id="amount"
                  type="number"
                  min={0}
                  step="0.01"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="currency">Moneda</Label>
                <Input
                  id="currency"
                  value={currency}
                  maxLength={3}
                  onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                  required
                />
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="description">Descripción</Label>
              <Textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
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

            <Button type="submit" disabled={!workflowId || createRequest.isPending} className="mt-2">
              {createRequest.isPending ? "Enviando…" : "Enviar solicitud"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
