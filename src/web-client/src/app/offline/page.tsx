import { CloudOff } from "lucide-react";

export const metadata = { title: "Geen verbinding - PakEchtMee" };

export default function OfflinePage() {
  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
      <CloudOff className="text-muted-foreground size-12" aria-hidden />
      <h1 className="text-xl font-semibold">Geen verbinding</h1>
      <p className="text-muted-foreground max-w-sm text-sm">
        PakEchtMee is geopend, maar je paklijst staat online. Zodra je weer verbinding hebt, kun je verder.
      </p>
    </main>
  );
}
