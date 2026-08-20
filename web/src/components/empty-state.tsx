import Image from "next/image";
import type { LucideIcon } from "lucide-react";

// Flip to true once /public/brand/empty-state.png exists.
const HAS_ILLUSTRATION = false;

export function EmptyState({
  icon: Icon,
  title,
  description,
}: {
  icon: LucideIcon;
  title: string;
  description?: string;
}) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      {HAS_ILLUSTRATION ? (
        <Image src="/brand/empty-state.png" alt="" width={140} height={140} className="mb-1" />
      ) : (
        <div className="flex size-14 items-center justify-center rounded-full bg-accent">
          <Icon className="size-6 text-accent-foreground" />
        </div>
      )}
      <div className="flex flex-col gap-1">
        <p className="font-medium">{title}</p>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
    </div>
  );
}
