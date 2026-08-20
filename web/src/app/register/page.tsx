"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { Building2 } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { ApiError } from "@/lib/api-client";
import { registerSchema, type RegisterInput } from "@/lib/validation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthLayout } from "@/components/auth-layout";

function slugify(value: string) {
  return value
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "");
}

export default function RegisterPage() {
  const { registerTenant } = useAuth();
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: { tenantName: "", adminName: "", adminEmail: "", adminPassword: "" },
  });

  const tenantName = watch("tenantName");

  async function onSubmit(values: RegisterInput) {
    setIsSubmitting(true);
    try {
      await registerTenant({
        tenantName: values.tenantName,
        tenantSlug: slugify(values.tenantName),
        adminName: values.adminName,
        adminEmail: values.adminEmail,
        adminPassword: values.adminPassword,
      });
      router.push("/");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear el tenant.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthLayout>
      <div className="glass-strong shadow-glow-lg w-full max-w-sm rounded-3xl p-8">
        <div className="mb-6 flex flex-col gap-1.5">
          <div className="mb-2 flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
            <Building2 className="size-5" />
          </div>
          <h1 className="text-2xl font-semibold tracking-tight">Crea tu empresa</h1>
          <p className="text-sm text-muted-foreground">
            Se crea un tenant nuevo y tu usuario queda como administrador.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="tenantName">Nombre de la empresa</Label>
            <Input id="tenantName" aria-invalid={!!errors.tenantName} {...register("tenantName")} />
            {errors.tenantName ? (
              <p className="text-xs text-destructive">{errors.tenantName.message}</p>
            ) : (
              tenantName && (
                <p className="text-xs text-muted-foreground">
                  Tu URL: <code>{slugify(tenantName) || "…"}</code>
                </p>
              )
            )}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="adminName">Tu nombre</Label>
            <Input id="adminName" aria-invalid={!!errors.adminName} {...register("adminName")} />
            {errors.adminName && <p className="text-xs text-destructive">{errors.adminName.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="adminEmail">Tu email</Label>
            <Input
              id="adminEmail"
              type="email"
              autoComplete="email"
              aria-invalid={!!errors.adminEmail}
              {...register("adminEmail")}
            />
            {errors.adminEmail && <p className="text-xs text-destructive">{errors.adminEmail.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="adminPassword">Contraseña</Label>
            <Input
              id="adminPassword"
              type="password"
              autoComplete="new-password"
              aria-invalid={!!errors.adminPassword}
              {...register("adminPassword")}
            />
            {errors.adminPassword ? (
              <p className="text-xs text-destructive">{errors.adminPassword.message}</p>
            ) : (
              <p className="text-xs text-muted-foreground">Mínimo 8 caracteres, una mayúscula y un número.</p>
            )}
          </div>
          <Button type="submit" disabled={isSubmitting} className="mt-2 h-10">
            {isSubmitting ? "Creando…" : "Crear empresa"}
          </Button>
        </form>

        <p className="mt-5 text-center text-sm text-muted-foreground">
          ¿Ya tienes cuenta?{" "}
          <Link href="/login" className="font-medium text-primary underline-offset-4 hover:underline">
            Inicia sesión
          </Link>
        </p>
      </div>
    </AuthLayout>
  );
}
