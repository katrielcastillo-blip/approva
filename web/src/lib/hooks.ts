import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type {
  ApproverType,
  BottleneckAnalytics,
  ConditionOperator,
  EscalationPolicy,
  PendingApproval,
  RequestDetail,
  RequestStatus,
  RequestSummary,
  UserDto,
  UserRole,
  WorkflowDefinitionDetail,
  WorkflowDefinitionSummary,
} from "@/lib/types";

export function useRequests(status?: RequestStatus) {
  return useQuery({
    queryKey: ["requests", status ?? "all"],
    queryFn: () => api.get<RequestSummary[]>(`/requests${status ? `?status=${status}` : ""}`),
  });
}

export function useRequest(id: string | undefined) {
  return useQuery({
    queryKey: ["requests", id],
    queryFn: () => api.get<RequestDetail>(`/requests/${id}`),
    enabled: !!id,
  });
}

export function usePendingApprovals() {
  return useQuery({
    queryKey: ["pending-approvals"],
    queryFn: () => api.get<PendingApproval[]>("/requests/pending-approvals"),
    refetchInterval: 30_000,
  });
}

export function useWorkflowDefinitions() {
  return useQuery({
    queryKey: ["workflow-definitions"],
    queryFn: () => api.get<WorkflowDefinitionSummary[]>("/workflow-definitions"),
  });
}

export function useWorkflowDefinition(id: string | undefined) {
  return useQuery({
    queryKey: ["workflow-definitions", id],
    queryFn: () => api.get<WorkflowDefinitionDetail>(`/workflow-definitions/${id}`),
    enabled: !!id,
  });
}

export function useUsers() {
  return useQuery({
    queryKey: ["users"],
    queryFn: () => api.get<UserDto[]>("/users"),
  });
}

export function useBottleneckAnalytics() {
  return useQuery({
    queryKey: ["analytics", "bottlenecks"],
    queryFn: () => api.get<BottleneckAnalytics>("/analytics/bottlenecks"),
  });
}

export function useCreateRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { workflowDefinitionId: string; title: string; amount: number; currency: string; payloadJson: string }) =>
      api.post<{ id: string }>("/requests", input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["requests"] });
      queryClient.invalidateQueries({ queryKey: ["pending-approvals"] });
    },
  });
}

export function useDecideRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      requestId,
      decision,
      comment,
    }: {
      requestId: string;
      decision: "Approve" | "Reject";
      comment?: string;
    }) => {
      const idempotencyKey = `${requestId}-${decision}-${crypto.randomUUID()}`;
      return api.post<{ requestId: string; requestStatus: string }>(
        `/requests/${requestId}/decisions`,
        { decision, comment },
        idempotencyKey
      );
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["requests", variables.requestId] });
      queryClient.invalidateQueries({ queryKey: ["requests"] });
      queryClient.invalidateQueries({ queryKey: ["pending-approvals"] });
    },
  });
}

export interface WorkflowConditionInput {
  field: string;
  operator: ConditionOperator;
  value: string;
}

export interface WorkflowStepInput {
  name: string;
  approverType: ApproverType;
  approverRef: string | null;
  slaHours: number;
  escalationPolicy: EscalationPolicy;
  conditions: WorkflowConditionInput[];
}

export function useCreateWorkflowDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { name: string; entityType: string; steps: WorkflowStepInput[] }) =>
      api.post<{ id: string }>("/workflow-definitions", input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-definitions"] });
    },
  });
}

export function useSetWorkflowDefinitionActive() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      api.post<void>(`/workflow-definitions/${id}/${isActive ? "activate" : "deactivate"}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-definitions"] });
    },
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: {
      email: string;
      name: string;
      password: string;
      role: UserRole;
      approverRole: string | null;
      managerId: string | null;
    }) => api.post<{ id: string }>("/users", input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
  });
}

export function useCancelRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (requestId: string) => api.post<void>(`/requests/${requestId}/cancel`),
    onSuccess: (_, requestId) => {
      queryClient.invalidateQueries({ queryKey: ["requests", requestId] });
      queryClient.invalidateQueries({ queryKey: ["requests"] });
    },
  });
}
