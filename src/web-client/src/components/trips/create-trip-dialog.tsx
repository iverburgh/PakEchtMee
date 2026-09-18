"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { createTripAction } from "@/lib/actions";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/field";

const schema = z
  .object({
    name: z.string().trim().min(1, "Geef het uitje een naam.").max(100, "Maximaal 100 tekens."),
    startDate: z.string(),
    endDate: z.string(),
  })
  .refine((value) => value.startDate === "" || value.endDate === "" || value.endDate >= value.startDate, {
    path: ["endDate"],
    message: "De einddatum kan niet voor de startdatum liggen.",
  });

type FormValues = z.infer<typeof schema>;

export function CreateTripDialog() {
  const [open, setOpen] = useState(false);
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
    defaultValues: { name: "", startDate: "", endDate: "" },
  });

  const submit = (values: FormValues) =>
    startTransition(async () => {
      const result = await createTripAction({
        name: values.name,
        startDate: values.startDate || null,
        endDate: values.endDate || null,
      });

      if (!result.ok) {
        setError("name", { message: result.error });

        return;
      }

      setOpen(false);
      reset();
      toast.success("Uitje aangemaakt.");
      router.push(`/trips/${result.value.id}/categorieen`);
    });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger render={<Button className="min-h-11" />}>
        <Plus aria-hidden />
        Nieuw uitje
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Nieuw uitje</DialogTitle>
          <DialogDescription>Datums zijn optioneel.</DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(submit)} className="flex flex-col gap-4">
          <Field id="trip-name" label="Naam" error={errors.name?.message}>
            <Input id="trip-name" placeholder="Zomer Frankrijk" autoFocus {...register("name")} />
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field id="trip-start" label="Van" error={errors.startDate?.message}>
              <Input id="trip-start" type="date" {...register("startDate")} />
            </Field>
            <Field id="trip-end" label="Tot" error={errors.endDate?.message}>
              <Input id="trip-end" type="date" {...register("endDate")} />
            </Field>
          </div>
          <DialogFooter>
            <Button type="submit" disabled={pending} className="min-h-11 w-full">
              {pending ? "Bezig..." : "Uitje aanmaken"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
