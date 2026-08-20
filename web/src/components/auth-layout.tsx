import Image from "next/image";
import { Logo } from "@/components/logo";
import { ThemeToggle } from "@/components/theme-toggle";

// Flip to true once /public/brand/login-illustration.png exists.
const HAS_ILLUSTRATION = false;

export function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-primary p-10 text-primary-foreground lg:flex">
        <div className="flex items-center gap-2 text-lg font-bold">
          <Logo className="size-7" />
          Approva
        </div>

        <div className="relative flex flex-1 items-center justify-center py-10">
          {HAS_ILLUSTRATION ? (
            <Image
              src="/brand/login-illustration.png"
              alt=""
              width={520}
              height={693}
              className="max-h-full w-auto max-w-full object-contain drop-shadow-2xl"
              priority
            />
          ) : (
            <Logo className="size-40 opacity-10" />
          )}
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
