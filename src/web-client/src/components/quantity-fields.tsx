"use client";

import { Controller, useFormContext } from "react-hook-form";
import { z } from "zod";
import { basisLabels, quantityBases, type QuantityBasis } from "@/lib/types";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { Field } from "@/components/field";

/** The quantity rule as the forms hold it: text inputs, so an empty field means "no quantity". */
export const quantityShape = {
  quantity: z
    .string()
    .trim()
    .refine((value) => value === "" || /^\d+$/.test(value), "Vul een heel getal in.")
    .refine((value) => value === "" || (Number(value) >= 1 && Number(value) <= 9999), "Kies een aantal tussen 1 en 9999."),
  unit: z.string().trim().max(20, "Maximaal 20 tekens."),
  basis: z.enum(["Fixed", "PerPerson", "PerAdult", "PerChild"]),
};

export type QuantityValues = { quantity: string; unit: string; basis: QuantityBasis };

/** The same rules as the API, so a mistake is shown next to the field instead of after a round trip. */
export function refineQuantity<S extends z.ZodType<QuantityValues, QuantityValues>>(schema: S): S {
  return schema
    .refine((value) => value.unit === "" || value.quantity !== "", {
      path: ["unit"],
      message: "Een eenheid kan alleen samen met een aantal.",
    })
    .refine((value) => value.basis === "Fixed" || value.quantity !== "", {
      path: ["quantity"],
      message: "Vul een aantal in voor een aantal per persoon, volwassene of kind.",
    });
}

export function toQuantityValues(input: { quantity: number | null; unit: string | null; basis: QuantityBasis } | undefined): QuantityValues {
  return {
    quantity: input?.quantity?.toString() ?? "",
    unit: input?.unit ?? "",
    basis: input?.basis ?? "Fixed",
  };
}

export function fromQuantityValues(values: QuantityValues): { quantity: number | null; unit: string | null; basis: QuantityBasis } {
  const quantity = values.quantity === "" ? null : Number(values.quantity);

  // Without a quantity there is nothing to count per head, so the basis falls back to fixed.
  return { quantity, unit: values.unit === "" ? null : values.unit, basis: quantity === null ? "Fixed" : values.basis };
}

/** Quantity, unit and basis; used inside a form that provides a react-hook-form context with these three fields. */
export function QuantityFields({ idPrefix }: { idPrefix: string }) {
  const {
    register,
    control,
    formState: { errors },
  } = useFormContext<QuantityValues>();

  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-3">
        <Field id={`${idPrefix}-quantity`} label="Aantal" error={errors.quantity?.message}>
          <Input id={`${idPrefix}-quantity`} inputMode="numeric" placeholder="1" {...register("quantity")} />
        </Field>
        <Field id={`${idPrefix}-unit`} label="Eenheid" error={errors.unit?.message}>
          <Input id={`${idPrefix}-unit`} placeholder="paar" {...register("unit")} />
        </Field>
      </div>
      <div className="flex flex-col gap-2">
        <Label id={`${idPrefix}-basis-label`}>Telt voor</Label>
        <Controller
          control={control}
          name="basis"
          render={({ field }) => (
            <ToggleGroup
              value={[field.value]}
              onValueChange={(value: string[]) => {
                // A single-choice group: pressing the active option again would empty it, so that is ignored.
                if (value.length > 0) {
                  field.onChange(value[value.length - 1] as QuantityBasis);
                }
              }}
              variant="outline"
              aria-labelledby={`${idPrefix}-basis-label`}
              className="grid w-full grid-cols-2 gap-1.5"
            >
              {quantityBases.map((basis) => (
                <ToggleGroupItem key={basis} value={basis} className="min-h-11 w-full text-sm">
                  {basisLabels[basis]}
                </ToggleGroupItem>
              ))}
            </ToggleGroup>
          )}
        />
      </div>
    </div>
  );
}
