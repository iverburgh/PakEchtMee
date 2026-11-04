"use client";

import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { changeFamilyCompositionAction } from "@/lib/actions";
import type { Composition } from "@/lib/types";
import { Card } from "@/components/ui/card";
import { CompositionForm } from "@/components/composition-form";

export function FamilyCompositionCard({ composition }: { composition: Composition }) {
  const router = useRouter();

  return (
    <Card className="flex flex-col gap-4 p-4">
      <div className="flex flex-col gap-1">
        <h2 className="text-base font-semibold tracking-tight">Wie horen bij het gezin?</h2>
        <p className="text-muted-foreground text-sm">
          Een nieuw uitje neemt dit over, zodat &ldquo;1 per kind&rdquo; meteen het juiste aantal geeft. Bestaande uitjes houden hun
          eigen samenstelling.
        </p>
      </div>
      <CompositionForm
        idPrefix="family-composition"
        initialValue={composition}
        submitLabel="Opslaan"
        onSubmit={changeFamilyCompositionAction}
        onSaved={() => {
          toast.success("Gezin opgeslagen.");
          router.refresh();
        }}
      />
    </Card>
  );
}
