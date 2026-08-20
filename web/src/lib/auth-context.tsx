"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { api, setToken, USER_STORAGE_KEY } from "@/lib/api-client";
import type { AuthResult } from "@/lib/types";

interface AuthContextValue {
  user: AuthResult | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  registerTenant: (input: {
    tenantName: string;
    tenantSlug: string;
    adminName: string;
    adminEmail: string;
    adminPassword: string;
  }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function persist(auth: AuthResult) {
  setToken(auth.token);
  window.localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(auth));
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const stored = window.localStorage.getItem(USER_STORAGE_KEY);
    if (stored) {
      try {
        setUser(JSON.parse(stored) as AuthResult);
      } catch {
        window.localStorage.removeItem(USER_STORAGE_KEY);
      }
    }
    setIsLoading(false);
  }, []);

  async function login(email: string, password: string) {
    const auth = await api.post<AuthResult>("/auth/login", { email, password });
    persist(auth);
    setUser(auth);
  }

  async function registerTenant(input: {
    tenantName: string;
    tenantSlug: string;
    adminName: string;
    adminEmail: string;
    adminPassword: string;
  }) {
    const auth = await api.post<AuthResult>("/auth/register-tenant", input);
    persist(auth);
    setUser(auth);
  }

  function logout() {
    setToken(null);
    window.localStorage.removeItem(USER_STORAGE_KEY);
    setUser(null);
    router.push("/login");
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, registerTenant, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}
