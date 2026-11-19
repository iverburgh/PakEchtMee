"use client";

import { useEffect, useTransition } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Minus, Plus } from "lucide-react";
import type { ActionResult } from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import type { TripPlan } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/field";

const maxCount = 20;

const count = z.number().int().min(0, "Minimaal 0.").max(maxCount, `Maximaal ${maxCount}.`);

// Dates are "" when empty, as a date input gives them; the same end-before-start rule as creating a trip.
const schema = z
  .object({ adults: count, children: count, startDate: z.string(), endDate: z.string() })
  .refine((value) => value.adults + value.children >= 1, {
    path: ["adults"],
    message: "Er moet minstens één persoon meegaan.",
  })
  .refine((value) => value.startDate === "" || value.endDate === "" || value.endDate >= value.startDate, {
    path: ["endDate"],
    message: "De einddatum kan niet voor de startdatum liggen.",
  });

type FormValues = z.infer<typeof schema>;

function toFormValues(plan: PlanInput): FormValues {
  return { adults: plan.adults, children: plan.children, startDate: plan.startDate ?? "", endDate: plan.endDate ?? "" };
}

/** The family has no dates, so they are optional on the way in. */
type PlanInput = { adults: number; children: number; startDate?: string | null; endDate?: string | null };

/** A − / + stepper: easier to hit on a phone than a number input, and it cannot go out of range. */
function Stepper({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span id={id} className="text-sm font-medium">
        {label}
      </span>
      <div role="group" aria-labelledby={id} className="flex items-center gap-2">
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-11"
          aria-label={`${label}: één minder`}
          disabled={value <= 0}
          onClick={() => onChange(value - 1)}
        >
          <Minus aria-hidden />
        </Button>
        <output aria-live="polite" className="w-8 text-center text-lg font-semibold tabular-nums">
          {value}
        </output>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-11"
          aria-label={`${label}: één meer`}
          disabled={value >= maxCount}
          onClick={() => onChange(value + 1)}
        >
          <Plus aria-hidden />
        </Button>
      </div>
    </div>
  );
}

/**
 * Number of adults and children, used for the family on the "Gezin" page, and with dates for a trip ("Wie en wanneer?").
 * The caller decides what saving means and what to say afterwards.
 */
export function CompositionForm<T>({
  idPrefix,
  initialValue,
  withDates = false,
  submitLabel,
  onSubmit,
  onSaved,
}: {
  idPrefix: string;
  initialValue: PlanInput;
  withDates?: boolean;
  submitLabel: string;
  onSubmit: (plan: TripPlan) => Promise<ActionResult<T>>;
  onSaved: (value: T) => void;
}) {
  const [pending, startTransition] = useTransition();
  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isDirty },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: toFormValues(initialValue) });

  // A saved value comes back through the page's props; start from it so "dirty" means "differs from what is stored".
  // The dependencies are the primitive values, so a fresh object with the same content does not reset the form.
  const { adults, children, startDate = null, endDate = null } = initialValue;
  useEffect(() => reset(toFormValues({ adults, children, startDate, endDate })), [adults, children, startDate, endDate, reset]);

  const submit = (values: FormValues) =>
    startTransition(async () => {
      const result = await callAction(() =>
        onSubmit({
          adults: values.adults,
          children: values.children,
          startDate: values.startDate || null,
          endDate: values.endDate || null,
        }),
      );

      if (!result.ok) {
        setError("adults", { message: result.error });

        return;
      }

      reset(values);
      onSaved(result.value);
    });

  return (
    <form onSubmit={handleSubmit(submit)} className="flex flex-col gap-4">
      <Controller
        control={control}
        name="adults"
        render={({ field }) => (
          <Stepper id={`${idPrefix}-adults`} label="Volwassenen" value={field.value} onChange={field.onChange} />
        )}
      />
      <Controller
        control={control}
        name="children"
        render={({ field }) => (
          <Stepper id={`${idPrefix}-children`} label="Kinderen" value={field.value} onChange={field.onChange} />
        )}
      />
      {withDates ? (
        <div className="grid grid-cols-2 gap-3">
          <Field id={`${idPrefix}-start`} label="Van" error={errors.startDate?.message}>
            <Input id={`${idPrefix}-start`} type="date" {...register("startDate")} />
          </Field>
          <Field id={`${idPrefix}-end`} label="Tot en met" error={errors.endDate?.message}>
            <Input id={`${idPrefix}-end`} type="date" {...register("endDate")} />
          </Field>
        </div>
      ) : null}
      {errors.adults?.message ?? errors.children?.message ? (
        <p role="alert" className="text-destructive text-sm">
          {errors.adults?.message ?? errors.children?.message}
        </p>
      ) : null}
      <Button type="submit" disabled={pending || !isDirty} className="min-h-11 w-full">
        {pending ? "Bezig..." : submitLabel}
      </Button>
    </form>
  );
}
