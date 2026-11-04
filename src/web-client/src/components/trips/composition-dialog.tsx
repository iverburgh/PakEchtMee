"use client";

import { useState } from "react";
import { Pencil, Users } from "lucide-react";
import { toast } from "sonner";
import { changeTripCompositionAction } from "@/lib/actions";
import { describeComposition, type Composition } from "@/lib/types";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { CompositionForm } from "@/components/composition-form";

/** The trip's "2 volwassenen · 3 kinderen" line, which opens a dialog to change who comes along on this trip only. */
export function CompositionDialog({ tripId, composition }: { tripId: string; composition: Composition }) {
  const [open, setOpen] = useState(false);

  const saved = ({ changedTotals }: { changedTotals: number }) => {
    setOpen(false);
    toast.success(
      changedTotals === 0
        ? "Samenstelling opgeslagen."
        : `Samenstelling opgeslagen; ${changedTotals} ${changedTotals === 1 ? "aantal" : "aantallen"} bij Te doen aangepast.`,
    );
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        className="text-muted-foreground hover:text-foreground -mx-1 inline-flex min-h-8 items-center gap-1.5 rounded-md px-1 text-xs transition-colors"
        aria-label={`Wie gaan er mee: ${describeComposition(composition)}. Wijzigen`}
      >
        <Users className="size-3.5" aria-hidden />
        {describeComposition(composition)}
        <Pencil className="size-3" aria-hidden />
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Wie gaan er mee?</DialogTitle>
          <DialogDescription>
            Geldt alleen voor dit uitje. Aantallen per persoon, volwassene of kind van spullen die nog Te doen zijn, worden opnieuw
            uitgerekend.
          </DialogDescription>
        </DialogHeader>
        <CompositionForm
          idPrefix="trip-composition"
          initialValue={composition}
          submitLabel="Opslaan"
          onSubmit={(value) => changeTripCompositionAction(tripId, value)}
          onSaved={saved}
        />
      </DialogContent>
    </Dialog>
  );
}
