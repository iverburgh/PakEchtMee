"use client";

import { useEffect, useTransition } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Minus, Plus } from "lucide-react";
import type { ActionResult } from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import type { Composition } from "@/lib/types";
import { Button } from "@/components/ui/button";

const maxCount = 20;

const count = z.number().int().min(0, "Minimaal 0.").max(maxCount, `Maximaal ${maxCount}.`);

const schema = z
  .object({ adults: count, children: count })
  .refine((value) => value.adults + value.children >= 1, {
    path: ["adults"],
    message: "Er moet minstens één persoon meegaan.",
  });

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
 * Number of adults and children, used for the family on the "Gezin" page and per trip. The caller decides what saving
 * means and what to say afterwards.
 */
export function CompositionForm<T>({
  idPrefix,
  initialValue,
  submitLabel,
  onSubmit,
  onSaved,
}: {
  idPrefix: string;
  initialValue: Composition;
  submitLabel: string;
  onSubmit: (composition: Composition) => Promise<ActionResult<T>>;
  onSaved: (value: T) => void;
}) {
  const [pending, startTransition] = useTransition();
  const {
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isDirty },
  } = useForm<Composition>({ resolver: zodResolver(schema), defaultValues: initialValue });

  // A saved value comes back through the page's props; start from it so "dirty" means "differs from what is stored".
  useEffect(() => reset(initialValue), [initialValue, reset]);

  const submit = (values: Composition) =>
    startTransition(async () => {
      const result = await callAction(() => onSubmit(values));

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
