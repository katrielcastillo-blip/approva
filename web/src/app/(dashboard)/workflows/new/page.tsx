"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Plus, Trash2, GripVertical } from "lucide-react";
import { useCreateWorkflowDefinition, useUsers, type WorkflowStepInput } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import type { ApproverType, ConditionOperator } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const APPROVER_TYPES: { value: ApproverType; label: string }[] = [
  { value: "Manager", label: "Manager directo del solicitante" },
  { value: "Role", label: "Rol de aprobador (ej. CFO)" },
  { value: "SpecificUser", label: "Usuario específico" },
];

const OPERATORS: { value: ConditionOperator; label: string }[] = [
  { value: "GreaterThan", label: "Mayor que (>)" },
  { value: "GreaterThanOrEqual", label: "Mayor o igual (>=)" },
  { value: "LessThan", label: "Menor que (<)" },
  { value: "LessThanOrEqual", label: "Menor o igual (<=)" },
  { value: "Equals", label: "Igual a (=)" },
  { value: "NotEquals", label: "Distinto de (≠)" },
  { value: "In", label: "Está en (lista separada por comas)" },
  { value: "NotIn", label: "No está en (lista separada por comas)" },
];

function emptyStep(): WorkflowStepInput {
  return {
    name: "",
    approverType: "Manager",
    approverRef: null,
    slaHours: 24,
    escalationPolicy: "EscalateToManager",
    conditions: [],
  };
}

export default function NewWorkflowPage() {
  const router = useRouter();
  const { data: users } = useUsers();
  const createWorkflow = useCreateWorkflowDefinition();

  const [name, setName] = useState("");
  const [entityType, setEntityType] = useState("PurchaseRequest");
  const [steps, setSteps] = useState<WorkflowStepInput[]>([emptyStep()]);

  const approverRoles = Array.from(
    new Set((users ?? []).map((u) => u.approverRole).filter((r): r is string => !!r))
  );

  function updateStep(index: number, patch: Partial<WorkflowStepInput>) {
    setSteps((prev) => prev.map((s, i) => (i === index ? { ...s, ...patch } : s)));
  }

  function addStep() {
    setSteps((prev) => [...prev, emptyStep()]);
  }

  function removeStep(index: number) {
    setSteps((prev) => prev.filter((_, i) => i !== index));
  }

  function addCondition(stepIndex: number) {
    setSteps((prev) =>
      prev.map((s, i) =>
        i === stepIndex
          ? { ...s, conditions: [...s.conditions, { field: "Amount", operator: "GreaterThan", value: "" }] }
          : s
      )
    );
  }

  function updateCondition(stepIndex: number, condIndex: number, patch: Partial<WorkflowStepInput["conditions"][number]>) {
    setSteps((prev) =>
      prev.map((s, i) =>
        i === stepIndex
          ? {
              ...s,
              conditions: s.conditions.map((c, j) => (j === condIndex ? { ...c, ...patch } : c)),
            }
          : s
      )
    );
  }

  function removeCondition(stepIndex: number, condIndex: number) {
    setSteps((prev) =>
      prev.map((s, i) => (i === stepIndex ? { ...s, conditions: s.conditions.filter((_, j) => j !== condIndex) } : s))
    );
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await createWorkflow.mutateAsync({ name, entityType, steps });
      toast.success("Flujo creado. Actívalo para empezar a usarlo.");
      router.push(`/workflows`);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear el flujo.");
    }
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6 pb-16">
      <div>
        <h1 className="text-2xl font-bold">Nuevo flujo de aprobación</h1>
        <p className="text-sm text-muted-foreground">
          Los pasos se evalúan en orden. Un paso sin condiciones siempre aplica; con condiciones, todas
          deben cumplirse (AND) para que el paso entre en el flujo.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="flex flex-col gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Información general</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Nombre del flujo</Label>
              <Input id="name" value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="entityType">Tipo de entidad</Label>
              <Input id="entityType" value={entityType} onChange={(e) => setEntityType(e.target.value)} required />
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-col gap-4">
          {steps.map((step, stepIndex) => (
            <Card key={stepIndex}>
              <CardHeader className="flex-row items-center justify-between">
                <CardTitle className="flex items-center gap-2 text-base">
                  <GripVertical className="size-4 text-muted-foreground" />
                  Paso {stepIndex + 1}
                </CardTitle>
                {steps.length > 1 && (
                  <Button type="button" variant="ghost" size="icon" onClick={() => removeStep(stepIndex)}>
                    <Trash2 className="size-4" />
                  </Button>
                )}
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1.5">
                    <Label>Nombre del paso</Label>
                    <Input
                      value={step.name}
                      onChange={(e) => updateStep(stepIndex, { name: e.target.value })}
                      placeholder="Aprobación CFO"
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label>SLA (horas)</Label>
                    <Input
                      type="number"
                      min={1}
                      value={step.slaHours}
                      onChange={(e) => updateStep(stepIndex, { slaHours: Number(e.target.value) })}
                      required
                    />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1.5">
                    <Label>Aprobador</Label>
                    <Select
                      value={step.approverType}
                      onValueChange={(v) =>
                        updateStep(stepIndex, { approverType: v as ApproverType, approverRef: null })
                      }
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {APPROVER_TYPES.map((t) => (
                          <SelectItem key={t.value} value={t.value}>
                            {t.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {step.approverType === "Role" && (
                    <div className="flex flex-col gap-1.5">
                      <Label>Rol (ej. CFO, CEO, HR Manager)</Label>
                      <Input
                        list={`roles-${stepIndex}`}
                        value={step.approverRef ?? ""}
                        onChange={(e) => updateStep(stepIndex, { approverRef: e.target.value })}
                        required
                      />
                      <datalist id={`roles-${stepIndex}`}>
                        {approverRoles.map((r) => (
                          <option key={r} value={r} />
                        ))}
                      </datalist>
                    </div>
                  )}

                  {step.approverType === "SpecificUser" && (
                    <div className="flex flex-col gap-1.5">
                      <Label>Usuario</Label>
                      <Select
                        value={step.approverRef ?? ""}
                        onValueChange={(v) => updateStep(stepIndex, { approverRef: v })}
                      >
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Selecciona un usuario" />
                        </SelectTrigger>
                        <SelectContent>
                          {users?.map((u) => (
                            <SelectItem key={u.id} value={u.id}>
                              {u.name} ({u.email})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}
                </div>

                <Separator />

                <div className="flex flex-col gap-2">
                  <div className="flex items-center justify-between">
                    <Label>Condiciones (todas deben cumplirse)</Label>
                    <Button type="button" variant="ghost" size="sm" onClick={() => addCondition(stepIndex)}>
                      <Plus className="size-3.5" />
                      Agregar condición
                    </Button>
                  </div>
                  {step.conditions.length === 0 && (
                    <p className="text-xs text-muted-foreground">
                      Sin condiciones: este paso siempre aplica.
                    </p>
                  )}
                  {step.conditions.map((cond, condIndex) => (
                    <div key={condIndex} className="flex items-center gap-2">
                      <Input
                        placeholder="Campo (ej. Amount, Department)"
                        value={cond.field}
                        onChange={(e) => updateCondition(stepIndex, condIndex, { field: e.target.value })}
                        className="flex-1"
                      />
                      <Select
                        value={cond.operator}
                        onValueChange={(v) => updateCondition(stepIndex, condIndex, { operator: v as ConditionOperator })}
                      >
                        <SelectTrigger className="w-48">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {OPERATORS.map((op) => (
                            <SelectItem key={op.value} value={op.value}>
                              {op.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <Input
                        placeholder="Valor"
                        value={cond.value}
                        onChange={(e) => updateCondition(stepIndex, condIndex, { value: e.target.value })}
                        className="flex-1"
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeCondition(stepIndex, condIndex)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        <Button type="button" variant="outline" onClick={addStep} className="self-start">
          <Plus className="size-4" />
          Agregar paso
        </Button>

        <Button type="submit" disabled={createWorkflow.isPending}>
          {createWorkflow.isPending ? "Guardando…" : "Crear flujo"}
        </Button>
      </form>
    </div>
  );
}
