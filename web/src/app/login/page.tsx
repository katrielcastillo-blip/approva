"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { LogIn } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api-client";
import { loginSchema, type LoginInput } from "@/lib/validation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthLayout } from "@/components/auth-layout";

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "admin@acme.test", password: "Demo1234!" },
  });

  async function onSubmit(values: LoginInput) {
    setIsSubmitting(true);
    try {
      await login(values.email, values.password);
      router.push("/");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo iniciar sesión.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthLayout>
      <div className="glass-strong shadow-glow-lg w-full max-w-sm rounded-3xl p-8">
        <div className="mb-6 flex flex-col gap-1.5">
          <div className="mb-2 flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
            <LogIn className="size-5" />
          </div>
          <h1 className="text-2xl font-semibold tracking-tight">Bienvenido de vuelta</h1>
          <p className="text-sm text-muted-foreground">Inicia sesión en tu tenant de Approva.</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              aria-invalid={!!errors.email}
              {...register("email")}
            />
            {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="password">Contraseña</Label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              aria-invalid={!!errors.password}
              {...register("password")}
            />
            {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
          </div>
          <Button type="submit" disabled={isSubmitting} className="mt-2 h-10">
            {isSubmitting ? "Entrando…" : "Entrar"}
          </Button>
        </form>

        <p className="mt-5 text-center text-sm text-muted-foreground">
          ¿Empresa nueva?{" "}
          <Link href="/register" className="font-medium text-primary underline-offset-4 hover:underline">
            Crea tu tenant
          </Link>
        </p>
        <p className="mt-4 rounded-xl border border-border/60 bg-muted/50 p-3 text-xs text-muted-foreground">
          Demo: <code className="font-medium text-foreground">admin@acme.test</code> /{" "}
          <code className="font-medium text-foreground">Demo1234!</code>
        </p>
      </div>
    </AuthLayout>
  );
}
