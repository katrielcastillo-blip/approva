import { z } from "zod";

export const loginSchema = z.object({
  email: z.string().min(1, "El email es obligatorio.").email("Ingresa un email válido."),
  password: z.string().min(1, "La contraseña es obligatoria."),
});
export type LoginInput = z.infer<typeof loginSchema>;

const passwordSchema = z
  .string()
  .min(8, "Mínimo 8 caracteres.")
  .regex(/[A-Z]/, "Incluye al menos una mayúscula.")
  .regex(/[0-9]/, "Incluye al menos un número.");

export const registerSchema = z.object({
  tenantName: z.string().min(2, "Mínimo 2 caracteres.").max(200, "Máximo 200 caracteres."),
  adminName: z.string().min(2, "Mínimo 2 caracteres.").max(200, "Máximo 200 caracteres."),
  adminEmail: z.string().min(1, "El email es obligatorio.").email("Ingresa un email válido."),
  adminPassword: passwordSchema,
});
export type RegisterInput = z.infer<typeof registerSchema>;

export const newRequestSchema = z.object({
  workflowDefinitionId: z.string().min(1, "Selecciona un flujo de aprobación."),
  title: z.string().min(3, "Mínimo 3 caracteres.").max(300, "Máximo 300 caracteres."),
  amount: z.coerce.number({ error: "Ingresa un monto." }).positive("El monto debe ser mayor a cero."),
  currency: z
    .string()
    .length(3, "Usa el código de 3 letras (ej. USD).")
    .regex(/^[A-Za-z]+$/, "Solo letras."),
  description: z.string().max(2000, "Máximo 2000 caracteres.").optional(),
});
export type NewRequestInput = z.infer<typeof newRequestSchema>;

export const newUserSchema = z.object({
  name: z.string().min(2, "Mínimo 2 caracteres.").max(200, "Máximo 200 caracteres."),
  email: z.string().min(1, "El email es obligatorio.").email("Ingresa un email válido."),
  password: passwordSchema,
  role: z.enum(["Requester", "Approver", "Admin"], { error: "Selecciona un rol." }),
  approverRole: z.string().max(100, "Máximo 100 caracteres.").optional(),
  managerId: z.string().optional(),
});
export type NewUserInput = z.infer<typeof newUserSchema>;

export const workflowConditionSchema = z.object({
  field: z.string().min(1, "El campo es obligatorio."),
  operator: z.enum([
    "Equals",
    "NotEquals",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "In",
    "NotIn",
  ]),
  value: z.string().min(1, "El valor es obligatorio."),
});

export const workflowStepSchema = z
  .object({
    name: z.string().min(2, "Mínimo 2 caracteres.").max(200, "Máximo 200 caracteres."),
    approverType: z.enum(["Manager", "Role", "SpecificUser"]),
    approverRef: z.string().nullable(),
    slaHours: z.coerce.number({ error: "Ingresa las horas de SLA." }).int().positive("Debe ser mayor a cero."),
    escalationPolicy: z.enum(["None", "EscalateToManager"]),
    conditions: z.array(workflowConditionSchema),
  })
  .refine((step) => step.approverType === "Manager" || !!step.approverRef?.trim(), {
    message: "Selecciona un aprobador para este paso.",
    path: ["approverRef"],
  });

export const newWorkflowSchema = z.object({
  name: z.string().min(2, "Mínimo 2 caracteres.").max(200, "Máximo 200 caracteres."),
  entityType: z.string().min(2, "Mínimo 2 caracteres.").max(100, "Máximo 100 caracteres."),
  steps: z.array(workflowStepSchema).min(1, "Agrega al menos un paso."),
});
export type NewWorkflowInput = z.infer<typeof newWorkflowSchema>;
