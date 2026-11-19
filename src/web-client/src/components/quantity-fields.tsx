"use client";

import { Controller, useFormContext, type FieldPath } from "react-hook-form";
import { z } from "zod";
import {
  basisLabels,
  periodLabels,
  quantityBases,
  quantityPeriods,
  type QuantityBasis,
  type QuantityPeriod,
} from "@/lib/types";
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
  period: z.enum(["PerTrip", "PerDay", "PerNight"]),
};

export type QuantityValues = { quantity: string; unit: string; basis: QuantityBasis; period: QuantityPeriod };

type QuantityRule = { quantity: number | null; unit: string | null; basis: QuantityBasis; period: QuantityPeriod };

/** The same rules as the API, so a mistake is shown next to the field instead of after a round trip. */
export function refineQuantity<S extends z.ZodType<QuantityValues, QuantityValues>>(schema: S): S {
  return schema
    .refine((value) => value.unit === "" || value.quantity !== "", {
      path: ["unit"],
      message: "Een eenheid kan alleen samen met een aantal.",
    })
    .refine((value) => (value.basis === "Fixed" && value.period === "PerTrip") || value.quantity !== "", {
      path: ["quantity"],
      message: "Vul een aantal in voor een aantal per persoon, volwassene, kind, dag of nacht.",
    });
}

export function toQuantityValues(input: QuantityRule | undefined): QuantityValues {
  return {
    quantity: input?.quantity?.toString() ?? "",
    unit: input?.unit ?? "",
    basis: input?.basis ?? "Fixed",
    period: input?.period ?? "PerTrip",
  };
}

export function fromQuantityValues(values: QuantityValues): QuantityRule {
  const quantity = values.quantity === "" ? null : Number(values.quantity);

  // Without a quantity there is nothing to count per head or per day, so the rule falls back to fixed once per trip.
  return quantity === null
    ? { quantity, unit: null, basis: "Fixed", period: "PerTrip" }
    : { quantity, unit: values.unit === "" ? null : values.unit, basis: values.basis, period: values.period };
}

/** A single-choice toggle group bound to one form field; pressing the active option again keeps it selected. */
function Choice<T extends string>({
  id,
  label,
  name,
  options,
  labels,
  columns,
}: {
  id: string;
  label: string;
  name: FieldPath<QuantityValues>;
  options: T[];
  labels: Record<T, string>;
  columns: string;
}) {
  const { control } = useFormContext<QuantityValues>();

  return (
    <div className="flex flex-col gap-2">
      <Label id={id}>{label}</Label>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <ToggleGroup
            value={[field.value]}
            onValueChange={(value: string[]) => {
              if (value.length > 0) {
                field.onChange(value[value.length - 1]);
              }
            }}
            variant="outline"
            aria-labelledby={id}
            className={`grid w-full gap-1.5 ${columns}`}
          >
            {options.map((option) => (
              <ToggleGroupItem key={option} value={option} className="min-h-11 w-full text-sm">
                {labels[option]}
              </ToggleGroupItem>
            ))}
          </ToggleGroup>
        )}
      />
    </div>
  );
}

/** Quantity, unit, basis and period; used inside a form that provides a react-hook-form context with these fields. */
export function QuantityFields({ idPrefix }: { idPrefix: string }) {
  const {
    register,
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
      <Choice
        id={`${idPrefix}-basis-label`}
        label="Telt voor"
        name="basis"
        options={quantityBases}
        labels={basisLabels}
        columns="grid-cols-2"
      />
      <Choice
        id={`${idPrefix}-period-label`}
        label="Hoe vaak"
        name="period"
        options={quantityPeriods}
        labels={periodLabels}
        columns="grid-cols-3"
      />
    </div>
  );
}
