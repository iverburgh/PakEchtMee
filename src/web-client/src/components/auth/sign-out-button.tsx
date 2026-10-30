"use client";

import { signOut } from "next-auth/react";
import { LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";

export function SignOutButton({ label = "Uitloggen" }: { label?: string }) {
  return (
    <Button variant="outline" className="h-11 w-full gap-2" onClick={() => void signOut({ callbackUrl: "/inloggen" })}>
      <LogOut className="size-4" aria-hidden />
      {label}
    </Button>
  );
}
