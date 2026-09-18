"use client";

import { useEffect, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import type { ActionResult } from "@/lib/actions";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/field";

/** Shared create/rename dialog; the server rules are mirrored here so errors show inline. */
export function NameDialog({
  open,
  onOpenChange,
  title,
  label,
  fieldId,
  placeholder,
  maxLength,
  initialValue = "",
  successMessage,
  onSubmit,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  label: string;
  fieldId: string;
  placeholder?: string;
  maxLength: number;
  initialValue?: string;
  successMessage: string;
  onSubmit: (name: string) => Promise<ActionResult<unknown>>;
}) {
  const [pending, startTransition] = useTransition();
  const router = useRouter();

  const schema = z.object({
    name: z
      .string()
      .trim()
      .min(1, `${label} is verplicht.`)
      .max(maxLength, `Maximaal ${maxLength} tekens.`),
  });

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { name: initialValue } });

  useEffect(() => {
    if (open) {
      reset({ name: initialValue });
    }
  }, [open, initialValue, reset]);

  const submit = (values: FormValues) =>
    startTransition(async () => {
      const result = await onSubmit(values.name);

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
          <Field id={fieldId} label={label} error={errors.name?.message}>
            <Input id={fieldId} placeholder={placeholder} autoFocus {...register("name")} />
          </Field>
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
