import Image from "next/image";
import { Logo } from "@/components/logo";
import { ThemeToggle } from "@/components/theme-toggle";

export function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-primary p-10 text-primary-foreground lg:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-40"
          style={{
            backgroundImage:
              "radial-gradient(at 20% 15%, rgba(255,255,255,0.35) 0px, transparent 45%), radial-gradient(at 90% 85%, rgba(255,255,255,0.2) 0px, transparent 50%)",
          }}
        />
        <div className="relative flex items-center gap-2 text-lg font-bold">
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

        <blockquote className="relative max-w-md text-sm text-primary-foreground/80">
          &ldquo;Cambia el comportamiento del sistema editando reglas en base de datos —
          sin que nadie recompile ni redespliegue nada.&rdquo;
        </blockquote>
      </div>

      <div className="bg-mesh relative flex items-center justify-center p-4">
        <div className="absolute top-4 right-4">
          <ThemeToggle />
        </div>
        {children}
      </div>
    </div>
  );
}
