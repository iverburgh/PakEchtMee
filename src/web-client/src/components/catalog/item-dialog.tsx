"use client";

import { useEffect, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import type { ActionResult } from "@/lib/actions";
import type { CatalogItem } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/field";

const schema = z
  .object({
    name: z.string().trim().min(1, "Naam is verplicht.").max(100, "Maximaal 100 tekens."),
    quantity: z
      .string()
      .trim()
      .refine((value) => value === "" || /^\d+$/.test(value), "Vul een heel getal in.")
      .refine(
        (value) => value === "" || (Number(value) >= 1 && Number(value) <= 9999),
        "Kies een aantal tussen 1 en 9999.",
      ),
    unit: z.string().trim().max(20, "Maximaal 20 tekens."),
  })
  .refine((value) => value.unit === "" || value.quantity !== "", {
    path: ["unit"],
    message: "Een eenheid kan alleen samen met een aantal.",
  });

type FormValues = z.infer<typeof schema>;

export type ItemInput = { name: string; quantity: number | null; unit: string | null };

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

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", quantity: "", unit: "" },
  });

  useEffect(() => {
    if (open) {
      reset({
        name: initialValue?.name ?? "",
        quantity: initialValue?.quantity?.toString() ?? "",
        unit: initialValue?.unit ?? "",
      });
    }
  }, [open, initialValue, reset]);

  const submit = (values: FormValues) =>
    startTransition(async () => {
      const result = await onSubmit({
        name: values.name,
        quantity: values.quantity === "" ? null : Number(values.quantity),
        unit: values.unit === "" ? null : values.unit,
      });

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
        <form onSubmit={handleSubmit(submit)} className="flex flex-col gap-4">
          <Field id="item-name" label="Naam" error={errors.name?.message}>
            <Input id="item-name" placeholder="Sokken" autoFocus {...register("name")} />
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field id="item-quantity" label="Aantal" error={errors.quantity?.message}>
              <Input id="item-quantity" inputMode="numeric" placeholder="7" {...register("quantity")} />
            </Field>
            <Field id="item-unit" label="Eenheid" error={errors.unit?.message}>
              <Input id="item-unit" placeholder="paar" {...register("unit")} />
            </Field>
          </div>
          <p className="text-muted-foreground text-xs">Aantal en eenheid zijn optioneel.</p>
          <DialogFooter>
            <Button type="submit" disabled={pending} className="min-h-11 w-full">
              {pending ? "Bezig..." : "Opslaan"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
