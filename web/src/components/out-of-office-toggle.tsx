"use client";

import { useState } from "react";
import { toast } from "sonner";
import { PlaneTakeoff } from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import { useUsers, useSetOutOfOffice } from "@/lib/hooks";
import { ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export function OutOfOfficeToggle() {
  const { user } = useAuth();
  const { data: users } = useUsers();
  const setOutOfOffice = useSetOutOfOffice();
  const [open, setOpen] = useState(false);
  const [delegateUserId, setDelegateUserId] = useState<string>("");

  const me = users?.find((u) => u.id === user?.userId);
  const isOutOfOffice = me?.isOutOfOffice ?? false;

  async function handleDisable() {
    try {
      await setOutOfOffice.mutateAsync({ isOutOfOffice: false, delegateUserId: null });
      toast.success("De vuelta en oficina.");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar.");
    }
  }

  async function handleEnable() {
    if (!delegateUserId) return;
    try {
      await setOutOfOffice.mutateAsync({ isOutOfOffice: true, delegateUserId });
      toast.success("Fuera de oficina activado — tus tareas se reasignan al delegado.");
      setOpen(false);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo activar.");
    }
  }

  const otherUsers = users?.filter((u) => u.id !== user?.userId) ?? [];

  return (
    <>
      {isOutOfOffice ? (
        <div className="flex items-center justify-between gap-2 rounded-md bg-amber-500/10 px-2 py-1.5 text-xs">
          <Badge variant="secondary" className="gap-1">
            <PlaneTakeoff className="size-3" />
            Fuera de oficina
          </Badge>
          <Button variant="ghost" size="sm" className="h-6 px-2 text-xs" onClick={handleDisable}>
            Volví
          </Button>
        </div>
      ) : (
        <Button variant="ghost" size="sm" className="w-full justify-start gap-2" onClick={() => setOpen(true)}>
          <PlaneTakeoff className="size-4" />
          Fuera de oficina
        </Button>
      )}

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="glass-strong">
          <DialogHeader>
            <DialogTitle>Activar fuera de oficina</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-1.5">
            <Label>Delegar mis tareas pendientes a</Label>
            <Select value={delegateUserId} onValueChange={setDelegateUserId}>
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecciona un delegado" />
              </SelectTrigger>
              <SelectContent>
                {otherUsers.map((u) => (
                  <SelectItem key={u.id} value={u.id}>
                    {u.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Las nuevas tareas que te asignen irán directo a esta persona mientras estés fuera.
            </p>
          </div>
          <DialogFooter>
            <Button onClick={handleEnable} disabled={!delegateUserId || setOutOfOffice.isPending}>
              Activar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
