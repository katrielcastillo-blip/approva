export type RequestStatus = "Draft" | "Pending" | "Approved" | "Rejected" | "Cancelled";
export type ApprovalTaskStatus = "Pending" | "Approved" | "Rejected" | "Escalated" | "Delegated" | "Skipped";
export type UserRole = "Requester" | "Approver" | "Admin";
export type ApproverType = "Role" | "SpecificUser" | "Manager";
export type ConditionOperator =
  | "Equals"
  | "NotEquals"
  | "GreaterThan"
  | "GreaterThanOrEqual"
  | "LessThan"
  | "LessThanOrEqual"
  | "In"
  | "NotIn";
export type EscalationPolicy = "None" | "EscalateToManager";

export interface AuthResult {
  token: string;
  userId: string;
  tenantId: string;
  email: string;
  name: string;
  role: UserRole;
}

export interface RequestSummary {
  id: string;
  title: string;
  amount: number;
  currency: string;
  status: RequestStatus;
  requesterId: string;
  requesterName: string;
  currentStepName: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface ApprovalTaskDto {
  id: string;
  stepName: string;
  assignedToUserId: string;
  assignedToUserName: string;
  status: ApprovalTaskStatus;
  assignedAt: string;
  dueAt: string;
  decidedAt: string | null;
  comment: string | null;
}

export interface AuditEventDto {
  id: string;
  eventType: string;
  actorId: string;
  actorName: string;
  payloadJson: string;
  occurredAt: string;
}

export interface RequestDetail {
  id: string;
  title: string;
  amount: number;
  currency: string;
  payloadJson: string;
  status: RequestStatus;
  requesterId: string;
  requesterName: string;
  workflowDefinitionId: string;
  workflowDefinitionName: string;
  createdAt: string;
  completedAt: string | null;
  tasks: ApprovalTaskDto[];
  auditTrail: AuditEventDto[];
}

export interface PendingApproval {
  taskId: string;
  requestId: string;
  requestTitle: string;
  amount: number;
  currency: string;
  requesterName: string;
  stepName: string;
  assignedAt: string;
  dueAt: string;
  isOverdue: boolean;
}

export interface WorkflowConditionDto {
  id: string;
  field: string;
  operator: ConditionOperator;
  value: string;
}

export interface WorkflowStepDto {
  id: string;
  order: number;
  name: string;
  approverType: ApproverType;
  approverRef: string | null;
  slaHours: number;
  escalationPolicy: EscalationPolicy;
  conditions: WorkflowConditionDto[];
}

export interface WorkflowDefinitionSummary {
  id: string;
  name: string;
  entityType: string;
  version: number;
  isActive: boolean;
  stepCount: number;
}

export interface WorkflowDefinitionDetail {
  id: string;
  name: string;
  entityType: string;
  version: number;
  isActive: boolean;
  steps: WorkflowStepDto[];
}

export interface UserDto {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  approverRole: string | null;
  managerId: string | null;
  managerName: string | null;
  isOutOfOffice: boolean;
  delegateUserId: string | null;
}

export interface StepBottleneck {
  stepName: string;
  decidedTaskCount: number;
  avgHoursToDecide: number;
  medianHoursToDecide: number;
  overdueCount: number;
}

export interface BottleneckAnalytics {
  steps: StepBottleneck[];
  slowestStepName: string | null;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
