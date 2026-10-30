import { ShieldX } from "lucide-react";
import { Card } from "@/components/ui/card";

/** What a member who is not an administrator sees on an administration page. */
export function AdministratorOnly() {
  return (
    <main className="px-4">
      <Card className="flex flex-col items-center gap-3 p-8 text-center">
        <ShieldX className="text-muted-foreground size-10" aria-hidden />
        <p className="text-muted-foreground text-sm">Alleen de beheerder kan deze pagina gebruiken.</p>
      </Card>
    </main>
  );
}
