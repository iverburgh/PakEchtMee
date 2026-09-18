"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Luggage, ListChecks } from "lucide-react";
import { cn } from "@/lib/utils";

const links = [
  { href: "/", label: "Uitjes", icon: Luggage, match: (path: string) => path === "/" || path.startsWith("/trips") },
  { href: "/catalogus", label: "Catalogus", icon: ListChecks, match: (path: string) => path.startsWith("/catalogus") },
];

export function BottomNav() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Hoofdnavigatie"
      className="bg-background/95 fixed inset-x-0 bottom-0 z-40 border-t backdrop-blur supports-[backdrop-filter]:bg-background/80 print:hidden"
    >
      <ul className="mx-auto flex w-full max-w-3xl items-stretch pb-[env(safe-area-inset-bottom)]">
        {links.map(({ href, label, icon: Icon, match }) => {
          const active = match(pathname);

          return (
            <li key={href} className="flex-1">
              <Link
                href={href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "flex min-h-14 flex-col items-center justify-center gap-1 text-xs font-medium transition-colors motion-reduce:transition-none",
                  active ? "text-primary" : "text-muted-foreground hover:text-foreground",
                )}
              >
                <Icon className="size-5" aria-hidden />
                {label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
