"use client";

import { useState } from "react";
import { CalendarDays, Pencil, Users } from "lucide-react";
import { toast } from "sonner";
import { changeTripPlanAction } from "@/lib/actions";
import { describeComposition, type TripPlan } from "@/lib/types";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { CompositionForm } from "@/components/composition-form";

/**
 * The trip's "4 - 18 jul · 2 volwassenen · 3 kinderen" line, which opens a dialog to change who comes along and when,
 * for this trip only. The date range is formatted by the server page, so it matches the rest of the header.
 */
export function PlanDialog({ tripId, plan, range }: { tripId: string; plan: TripPlan; range: string }) {
  const [open, setOpen] = useState(false);
  const composition = describeComposition(plan);

  const saved = ({ changedTotals }: { changedTotals: number }) => {
    setOpen(false);
    toast.success(
      changedTotals === 0
        ? "Uitje opgeslagen."
        : `Uitje opgeslagen; ${changedTotals} ${changedTotals === 1 ? "aantal" : "aantallen"} bij Te doen aangepast.`,
    );
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        className="text-muted-foreground hover:text-foreground -mx-1 inline-flex min-h-8 flex-wrap items-center gap-x-3 gap-y-0.5 rounded-md px-1 text-left text-xs transition-colors"
        aria-label={`Wie en wanneer: ${range || "geen datums"}, ${composition}. Wijzigen`}
      >
        <span className="inline-flex items-center gap-1.5">
          <CalendarDays className="size-3.5" aria-hidden />
          {range || "Geen datums"}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <Users className="size-3.5" aria-hidden />
          {composition}
          <Pencil className="size-3" aria-hidden />
        </span>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Wie en wanneer?</DialogTitle>
          <DialogDescription>
            Geldt alleen voor dit uitje. Aantallen per persoon, dag of nacht van spullen die nog Te doen zijn, worden opnieuw
            uitgerekend.
          </DialogDescription>
        </DialogHeader>
        <CompositionForm
          idPrefix="trip-plan"
          initialValue={plan}
          withDates
          submitLabel="Opslaan"
          onSubmit={(value) => changeTripPlanAction(tripId, value)}
          onSaved={saved}
        />
      </DialogContent>
    </Dialog>
  );
}
