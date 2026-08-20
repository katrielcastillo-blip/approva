"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useUsers, useCreateUser } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { roleLabel } from "@/lib/format";
import type { UserRole } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Plus } from "lucide-react";

export default function UsersPage() {
  const { data: users, isLoading } = useUsers();
  const createUser = useCreateUser();
  const [open, setOpen] = useState(false);

  const [email, setEmail] = useState("");
  const [name, setName] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>("Requester");
  const [approverRole, setApproverRole] = useState("");
  const [managerId, setManagerId] = useState<string>("none");

  function resetForm() {
    setEmail("");
    setName("");
    setPassword("");
    setRole("Requester");
    setApproverRole("");
    setManagerId("none");
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await createUser.mutateAsync({
        email,
        name,
        password,
        role,
        approverRole: approverRole || null,
        managerId: managerId === "none" ? null : managerId,
      });
      toast.success("Usuario creado.");
      setOpen(false);
      resetForm();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear el usuario.");
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Usuarios</h1>
          <p className="text-sm text-muted-foreground">Gestiona quién puede solicitar y aprobar.</p>
        </div>
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="size-4" />
              Nuevo usuario
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Nuevo usuario</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="u-name">Nombre</Label>
                <Input id="u-name" value={name} onChange={(e) => setName(e.target.value)} required />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="u-email">Email</Label>
                <Input id="u-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="u-password">Contraseña</Label>
                <Input
                  id="u-password"
                  type="password"
                  minLength={8}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Rol</Label>
                <Select value={role} onValueChange={(v) => setRole(v as UserRole)}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Requester">Solicitante</SelectItem>
                    <SelectItem value="Approver">Aprobador</SelectItem>
                    <SelectItem value="Admin">Administrador</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="u-approverRole">Rol de aprobador (opcional, ej. CFO)</Label>
                <Input id="u-approverRole" value={approverRole} onChange={(e) => setApproverRole(e.target.value)} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Manager (opcional)</Label>
                <Select value={managerId} onValueChange={setManagerId}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sin manager</SelectItem>
                    {users?.map((u) => (
                      <SelectItem key={u.id} value={u.id}>
                        {u.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <DialogFooter>
                <Button type="submit" disabled={createUser.isPending}>
                  {createUser.isPending ? "Creando…" : "Crear usuario"}
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {isLoading && <Skeleton className="h-64 w-full" />}

      {!isLoading && (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Rol</TableHead>
              <TableHead>Rol de aprobador</TableHead>
              <TableHead>Manager</TableHead>
              <TableHead>Estado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users?.map((u) => (
              <TableRow key={u.id}>
                <TableCell className="font-medium">{u.name}</TableCell>
                <TableCell className="text-muted-foreground">{u.email}</TableCell>
                <TableCell>
                  <Badge variant="outline">{roleLabel(u.role)}</Badge>
                </TableCell>
                <TableCell className="text-muted-foreground">{u.approverRole ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground">{u.managerName ?? "—"}</TableCell>
                <TableCell>
                  {u.isOutOfOffice ? <Badge variant="secondary">Fuera de oficina</Badge> : "Activo"}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
