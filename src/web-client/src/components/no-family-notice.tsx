import Link from "next/link";
import { ShieldCheck } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

/** For an administrator who belongs to no family: there are no own trips or categories, only the administration. */
export function NoFamilyNotice() {
  return (
    <Card className="flex flex-col items-center gap-3 p-8 text-center">
      <ShieldCheck className="text-muted-foreground size-10" aria-hidden />
      <p className="text-muted-foreground text-sm">
        Je bent beheerder, maar geen lid van een familie. Uitjes en een eigen catalogus horen bij een familie.
      </p>
      <Button nativeButton={false} render={<Link href="/beheer" />}>
        Naar beheer
      </Button>
    </Card>
  );
}
