import Image from "next/image";

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      <Image src="/brand/empty-state.png" alt="" width={140} height={140} className="mb-1" />
      <div className="flex flex-col gap-1">
        <p className="font-medium">{title}</p>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
    </div>
  );
}
