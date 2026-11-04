"use client";

import { useEffect, useTransition } from "react";
import { useRouter } from "next/navigation";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import type { ActionResult } from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import type { CatalogItem, QuantityBasis } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/field";
import { QuantityFields, fromQuantityValues, quantityShape, refineQuantity, toQuantityValues } from "@/components/quantity-fields";

const schema = refineQuantity(
  z.object({
    name: z.string().trim().min(1, "Naam is verplicht.").max(100, "Maximaal 100 tekens."),
    ...quantityShape,
  }),
);

type FormValues = z.infer<typeof schema>;

export type ItemInput = { name: string; quantity: number | null; unit: string | null; basis: QuantityBasis };

export function ItemDialog({
  open,
  onOpenChange,
  title,
  initialValue,
  successMessage,
  onSubmit,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  initialValue?: CatalogItem;
  successMessage: string;
  onSubmit: (values: ItemInput) => Promise<ActionResult<unknown>>;
}) {
  const [pending, startTransition] = useTransition();
  const router = useRouter();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", ...toQuantityValues(undefined) },
  });
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = form;

  useEffect(() => {
    if (open) {
      reset({ name: initialValue?.name ?? "", ...toQuantityValues(initialValue) });
    }
  }, [open, initialValue, reset]);

  const submit = (values: FormValues) =>
    startTransition(async () => {
      const result = await callAction(() =>
        onSubmit({ name: values.name, ...fromQuantityValues(values) }),
      );

      if (!result.ok) {
        setError("name", { message: result.error });

        return;
      }

      toast.success(successMessage);
      onOpenChange(false);
      router.refresh();
    });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <FormProvider {...form}>
          <form onSubmit={handleSubmit(submit)} className="flex flex-col gap-4">
            <Field id="item-name" label="Naam" error={errors.name?.message}>
              <Input id="item-name" placeholder="Sokken" autoFocus {...register("name")} />
            </Field>
            <QuantityFields idPrefix="item" />
            <p className="text-muted-foreground text-xs">
              Aantal en eenheid zijn optioneel. Kies &ldquo;Per kind&rdquo; of &ldquo;Per persoon&rdquo; om het aantal bij een uitje
              uit te rekenen.
            </p>
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
