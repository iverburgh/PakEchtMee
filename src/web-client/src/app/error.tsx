"use client";

import { CloudOff } from "lucide-react";
import { Button } from "@/components/ui/button";

/** Every data failure lands here, so a missing connection reads as a retryable state rather than a crash. */
export default function ErrorState({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
      <CloudOff className="text-muted-foreground size-12" aria-hidden />
      <h1 className="text-xl font-semibold">Gegevens niet geladen</h1>
      <p className="text-muted-foreground max-w-sm text-sm">
        We konden je paklijst niet ophalen. Controleer je verbinding en probeer het opnieuw.
      </p>
      <Button onClick={reset} className="min-h-11 px-6">
        Opnieuw proberen
      </Button>
    </main>
  );
}
