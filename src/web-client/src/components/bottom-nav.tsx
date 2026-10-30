"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { signOut } from "next-auth/react";
import { CircleUserRound, ListChecks, LogOut, Luggage, ShieldCheck } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const memberLinks = [
  { href: "/", label: "Uitjes", icon: Luggage, match: (path: string) => path === "/" || path.startsWith("/trips") },
  { href: "/catalogus", label: "Catalogus", icon: ListChecks, match: (path: string) => path.startsWith("/catalogus") },
];

const administratorLink = {
  href: "/beheer",
  label: "Beheer",
  icon: ShieldCheck,
  match: (path: string) => path.startsWith("/beheer"),
};

const tabClass = "flex min-h-14 w-full flex-col items-center justify-center gap-1 text-xs font-medium transition-colors motion-reduce:transition-none";

export function BottomNav({
  email,
  familyName,
  isAdministrator,
}: {
  email: string;
  familyName: string | null;
  isAdministrator: boolean;
}) {
  const pathname = usePathname();
  const links = isAdministrator ? [...memberLinks, administratorLink] : memberLinks;

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
                className={cn(tabClass, active ? "text-primary" : "text-muted-foreground hover:text-foreground")}
              >
                <Icon className="size-5" aria-hidden />
                {label}
              </Link>
            </li>
          );
        })}
        <li className="flex-1">
          <DropdownMenu>
            <DropdownMenuTrigger className={cn(tabClass, "text-muted-foreground hover:text-foreground")}>
              <CircleUserRound className="size-5" aria-hidden />
              Account
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" side="top" className="min-w-60">
              <DropdownMenuGroup>
                <DropdownMenuLabel className="flex flex-col gap-0.5">
                  <span className="text-foreground truncate font-medium">{email}</span>
                  <span className="text-muted-foreground text-xs font-normal">
                    {familyName ? `Familie ${familyName}` : "Geen familie"}
                    {isAdministrator ? " · beheerder" : ""}
                  </span>
                </DropdownMenuLabel>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => void signOut({ callbackUrl: "/inloggen" })}>
                <LogOut aria-hidden />
                Uitloggen
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </li>
      </ul>
    </nav>
  );
}
