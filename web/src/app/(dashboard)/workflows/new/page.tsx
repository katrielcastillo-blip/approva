"use client";

import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { useForm, useFieldArray, Controller, type Control, type UseFormRegister, type FieldErrors } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import type { z } from "zod";
import { Plus, Trash2, GripVertical, Workflow as WorkflowIcon } from "lucide-react";
import { useCreateWorkflowDefinition, useUsers } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { newWorkflowSchema, type NewWorkflowInput } from "@/lib/validation";

type FormValues = z.input<typeof newWorkflowSchema>;
import type { ApproverType, ConditionOperator } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { PageHeader } from "@/components/page-header";
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

function emptyStep() {
  return {
    name: "",
    approverType: "Manager" as ApproverType,
    approverRef: null as string | null,
    slaHours: 24,
    escalationPolicy: "EscalateToManager" as const,
    conditions: [] as { field: string; operator: ConditionOperator; value: string }[],
  };
}

export default function NewWorkflowPage() {
  const router = useRouter();
  const { data: users } = useUsers();
  const createWorkflow = useCreateWorkflowDefinition();

  const {
    control,
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormValues, unknown, NewWorkflowInput>({
    resolver: zodResolver(newWorkflowSchema),
    defaultValues: { name: "", entityType: "PurchaseRequest", steps: [emptyStep()] },
  });

  const { fields: stepFields, append: appendStep, remove: removeStep } = useFieldArray({ control, name: "steps" });

  const approverRoles = Array.from(
    new Set((users ?? []).map((u) => u.approverRole).filter((r): r is string => !!r))
  );

  async function onSubmit(values: NewWorkflowInput) {
    try {
      await createWorkflow.mutateAsync(values);
      toast.success("Flujo creado. Actívalo para empezar a usarlo.");
      router.push("/workflows");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear el flujo.");
    }
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6 pb-16">
      <PageHeader
        icon={WorkflowIcon}
        title="Nuevo flujo de aprobación"
        description="Los pasos se evalúan en orden. Un paso sin condiciones siempre aplica; con condiciones, todas deben cumplirse (AND) para que el paso entre en el flujo."
      />

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        <Card className="glass rounded-2xl border-border/60">
          <CardHeader>
            <CardTitle className="text-base">Información general</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Nombre del flujo</Label>
              <Input id="name" aria-invalid={!!errors.name} {...register("name")} />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="entityType">Tipo de entidad</Label>
              <Input id="entityType" aria-invalid={!!errors.entityType} {...register("entityType")} />
              {errors.entityType && <p className="text-xs text-destructive">{errors.entityType.message}</p>}
            </div>
          </CardContent>
        </Card>

        {errors.steps?.root?.message && (
          <p className="text-sm text-destructive">{errors.steps.root.message}</p>
        )}
        {typeof errors.steps?.message === "string" && (
          <p className="text-sm text-destructive">{errors.steps.message}</p>
        )}

        <div className="flex flex-col gap-4">
          {stepFields.map((stepField, stepIndex) => (
            <StepCard
              key={stepField.id}
              control={control}
              register={register}
              stepIndex={stepIndex}
              canRemove={stepFields.length > 1}
              onRemove={() => removeStep(stepIndex)}
              approverType={watch(`steps.${stepIndex}.approverType`)}
              approverRoles={approverRoles}
              users={users}
              errors={errors}
            />
          ))}
        </div>

        <Button type="button" variant="outline" onClick={() => appendStep(emptyStep())} className="self-start">
          <Plus className="size-4" />
          Agregar paso
        </Button>

        <Button type="submit" disabled={createWorkflow.isPending} className="shadow-glow h-10">
          {createWorkflow.isPending ? "Guardando…" : "Crear flujo"}
        </Button>
      </form>
    </div>
  );
}

function StepCard({
  control,
  register,
  stepIndex,
  canRemove,
  onRemove,
  approverType,
  approverRoles,
  users,
  errors,
}: {
  control: Control<FormValues, unknown, NewWorkflowInput>;
  register: UseFormRegister<FormValues>;
  stepIndex: number;
  canRemove: boolean;
  onRemove: () => void;
  approverType: ApproverType;
  approverRoles: string[];
  users: { id: string; name: string; email: string }[] | undefined;
  errors: FieldErrors<FormValues>;
}) {
  const { fields: conditionFields, append: appendCondition, remove: removeCondition } = useFieldArray({
    control,
    name: `steps.${stepIndex}.conditions`,
  });

  const stepErrors = errors.steps?.[stepIndex];

  return (
    <Card className="glass rounded-2xl border-border/60">
      <CardHeader className="flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2 text-base">
          <GripVertical className="size-4 text-muted-foreground" />
          Paso {stepIndex + 1}
        </CardTitle>
        {canRemove && (
          <Button type="button" variant="ghost" size="icon" onClick={onRemove}>
            <Trash2 className="size-4" />
          </Button>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <Label>Nombre del paso</Label>
            <Input
              placeholder="Aprobación CFO"
              aria-invalid={!!stepErrors?.name}
              {...register(`steps.${stepIndex}.name`)}
            />
            {stepErrors?.name && <p className="text-xs text-destructive">{stepErrors.name.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>SLA (horas)</Label>
            <Input
              type="number"
              min={1}
              aria-invalid={!!stepErrors?.slaHours}
              {...register(`steps.${stepIndex}.slaHours`)}
            />
            {stepErrors?.slaHours && <p className="text-xs text-destructive">{stepErrors.slaHours.message}</p>}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <Label>Aprobador</Label>
            <Controller
              control={control}
              name={`steps.${stepIndex}.approverType`}
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={(v) => {
                    field.onChange(v);
                  }}
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
              )}
            />
          </div>

          {approverType === "Role" && (
            <div className="flex flex-col gap-1.5">
              <Label>Rol (ej. CFO, CEO, HR Manager)</Label>
              <Input
                list={`roles-${stepIndex}`}
                aria-invalid={!!stepErrors?.approverRef}
                {...register(`steps.${stepIndex}.approverRef`)}
              />
              <datalist id={`roles-${stepIndex}`}>
                {approverRoles.map((r) => (
                  <option key={r} value={r} />
                ))}
              </datalist>
              {stepErrors?.approverRef && (
                <p className="text-xs text-destructive">{stepErrors.approverRef.message}</p>
              )}
            </div>
          )}

          {approverType === "SpecificUser" && (
            <div className="flex flex-col gap-1.5">
              <Label>Usuario</Label>
              <Controller
                control={control}
                name={`steps.${stepIndex}.approverRef`}
                render={({ field }) => (
                  <Select value={field.value ?? ""} onValueChange={field.onChange}>
                    <SelectTrigger className="w-full" aria-invalid={!!stepErrors?.approverRef}>
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
                )}
              />
              {stepErrors?.approverRef && (
                <p className="text-xs text-destructive">{stepErrors.approverRef.message}</p>
              )}
            </div>
          )}
        </div>

        <Separator />

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <Label>Condiciones (todas deben cumplirse)</Label>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => appendCondition({ field: "Amount", operator: "GreaterThan", value: "" })}
            >
              <Plus className="size-3.5" />
              Agregar condición
            </Button>
          </div>
          {conditionFields.length === 0 && (
            <p className="text-xs text-muted-foreground">Sin condiciones: este paso siempre aplica.</p>
          )}
          {conditionFields.map((condField, condIndex) => {
            const condErrors = stepErrors?.conditions?.[condIndex];
            return (
              <div key={condField.id} className="flex flex-col gap-1">
                <div className="flex items-center gap-2">
                  <Input
                    placeholder="Campo (ej. Amount, Department)"
                    className="flex-1"
                    aria-invalid={!!condErrors?.field}
                    {...register(`steps.${stepIndex}.conditions.${condIndex}.field`)}
                  />
                  <Controller
                    control={control}
                    name={`steps.${stepIndex}.conditions.${condIndex}.operator`}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
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
                    )}
                  />
                  <Input
                    placeholder="Valor"
                    className="flex-1"
                    aria-invalid={!!condErrors?.value}
                    {...register(`steps.${stepIndex}.conditions.${condIndex}.value`)}
                  />
                  <Button type="button" variant="ghost" size="icon" onClick={() => removeCondition(condIndex)}>
                    <Trash2 className="size-4" />
                  </Button>
                </div>
                {(condErrors?.field || condErrors?.value) && (
                  <p className="text-xs text-destructive">
                    {condErrors?.field?.message ?? condErrors?.value?.message}
                  </p>
                )}
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
