import type { ProblemDetails } from "@/lib/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
const TOKEN_STORAGE_KEY = "approva.token";
export const USER_STORAGE_KEY = "approva.user";

export class ApiError extends Error {
  status: number;
  problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null, fallbackMessage: string) {
    super(problem?.detail ?? fallbackMessage);
    this.status = status;
    this.problem = problem;
  }
}

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string | null): void {
  if (typeof window === "undefined") return;
  if (token) window.localStorage.setItem(TOKEN_STORAGE_KEY, token);
  else window.localStorage.removeItem(TOKEN_STORAGE_KEY);
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  idempotencyKey?: string;
  skipAuth?: boolean;
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };

  if (!options.skipAuth) {
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  if (options.idempotencyKey) headers["Idempotency-Key"] = options.idempotencyKey;

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (response.status === 204) return undefined as T;

  const isJson = response.headers.get("content-type")?.includes("application/json");
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    if (response.status === 401 && typeof window !== "undefined") {
      // The token's signature can still be structurally valid while naming a user/tenant
      // that no longer exists (deleted, or a local dev database reset regenerated all
      // IDs) — the backend rejects those at authentication time. Rather than leaving the
      // app "logged in" with every list silently empty, force a clean re-login.
      const wasLoggedIn = !!getToken();
      setToken(null);
      window.localStorage.removeItem(USER_STORAGE_KEY);
      if (wasLoggedIn && window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }
    throw new ApiError(response.status, data as ProblemDetails | null, response.statusText);
  }

  return data as T;
}

export const api = {
  get: <T>(path: string) => apiFetch<T>(path),
  post: <T>(path: string, body?: unknown, idempotencyKey?: string) =>
    apiFetch<T>(path, { method: "POST", body, idempotencyKey }),
};
