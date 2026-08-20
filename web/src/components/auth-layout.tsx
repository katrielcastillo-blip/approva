import Image from "next/image";
import { Logo } from "@/components/logo";
import { ThemeToggle } from "@/components/theme-toggle";

export function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-primary p-10 text-primary-foreground lg:flex">
        <div className="flex items-center gap-2 text-lg font-bold">
          <Logo className="size-7 rounded" />
          Approva
        </div>

        <div className="relative flex flex-1 items-center justify-center py-10">
          <Image
            src="/brand/login-illustration.jpg"
            alt="Solicitud avanzando a través de una cadena de verificaciones de aprobación"
            width={1568}
            height={1336}
            className="max-h-[420px] w-auto max-w-full rounded-2xl object-contain shadow-2xl"
            priority
          />
        </div>

        <blockquote className="max-w-md text-sm text-primary-foreground/80">
          &ldquo;Cambia el comportamiento del sistema editando reglas en base de datos —
          sin que nadie recompile ni redespliegue nada.&rdquo;
        </blockquote>
      </div>

      <div className="relative flex items-center justify-center bg-muted/30 p-4">
        <div className="absolute top-4 right-4">
          <ThemeToggle />
        </div>
        {children}
      </div>
    </div>
  );
}
