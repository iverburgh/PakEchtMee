"use client";

import { useEffect, useTransition } from "react";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { changeTripItemQuantityAction } from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import { describeAmount, describeComposition, type Composition, type TripItem } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { QuantityFields, fromQuantityValues, quantityShape, refineQuantity, toQuantityValues, type QuantityValues } from "@/components/quantity-fields";

const schema = refineQuantity(z.object(quantityShape));

/** Changes one trip item's quantity rule; only offered while the item is still to do. The catalog item stays as it is. */
export function QuantityDialog({
  tripId,
  item,
  composition,
  onOpenChange,
}: {
  tripId: string;
  item: TripItem | null;
  composition: Composition;
  onOpenChange: (open: boolean) => void;
}) {
  const [pending, startTransition] = useTransition();
  const form = useForm<QuantityValues>({ resolver: zodResolver(schema), defaultValues: toQuantityValues(undefined) });
  const { handleSubmit, reset, setError } = form;

  useEffect(() => {
    if (item) {
      reset(toQuantityValues(item));
    }
  }, [item, reset]);

  if (item === null) {
    return null;
  }

  const submit = (values: QuantityValues) =>
    startTransition(async () => {
      const result = await callAction(() => changeTripItemQuantityAction(tripId, item.id, fromQuantityValues(values)));

      if (!result.ok) {
        setError("quantity", { message: result.error });

        return;
      }

      const total = describeAmount(result.value.total, result.value.unit);
      toast.success(total ? `${item.name}: ${total}` : `${item.name}: geen aantal`);
      onOpenChange(false);
    });

  return (
    <Dialog open onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Aantal {item.name}</DialogTitle>
          <DialogDescription>
            Alleen voor dit uitje ({describeComposition(composition)}). Laat het aantal leeg voor geen aantal.
          </DialogDescription>
        </DialogHeader>
        <FormProvider {...form}>
          <form onSubmit={handleSubmit(submit)} className="flex flex-col gap-4">
            <QuantityFields idPrefix="trip-item" />
            <DialogFooter>
              <Button type="submit" disabled={pending} className="min-h-11 w-full">
                {pending ? "Bezig..." : "Opslaan"}
              </Button>
            </DialogFooter>
          </form>
        </FormProvider>
      </DialogContent>
    </Dialog>
  );
}
