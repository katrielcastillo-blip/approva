import Image from "next/image";

export function Logo({ className }: { className?: string }) {
  return <Image src="/brand/logomark.png" alt="" width={32} height={32} className={className} priority />;
}
