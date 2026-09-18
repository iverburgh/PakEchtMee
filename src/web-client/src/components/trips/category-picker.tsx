"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import type { Category, TripCategory, TripDetails } from "@/lib/types";
import { removeTripCategoryAction, selectTripCategoriesAction } from "@/lib/actions";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";

export function CategoryPicker({ trip, categories }: { trip: TripDetails; categories: Category[] }) {
  const selected = new Set(trip.categories.map((category) => category.categoryId));
  const [pendingAdditions, setPendingAdditions] = useState<string[]>([]);
  const [confirming, setConfirming] = useState<TripCategory | null>(null);
  const [pending, startTransition] = useTransition();
  const router = useRouter();

  const toggle = (categoryId: string, checked: boolean) => {
    if (!selected.has(categoryId)) {
      setPendingAdditions((current) =>
        checked ? [...current, categoryId] : current.filter((id) => id !== categoryId),
      );

      return;
    }

    const tripCategory = trip.categories.find((category) => category.categoryId === categoryId)!;

    if (tripCategory.hasProgressedItems) {
      setConfirming(tripCategory);

      return;
    }

    removeCategory(categoryId);
  };

  const removeCategory = (categoryId: string) =>
    startTransition(async () => {
      const result = await removeTripCategoryAction(trip.id, categoryId);

      if (!result.ok) {
        toast.error(result.error);

        return;
      }

      setConfirming(null);
      toast.success("Categorie van de paklijst gehaald.");
      router.refresh();
    });

  const addSelected = () =>
    startTransition(async () => {
      const result = await selectTripCategoriesAction(trip.id, pendingAdditions);

      if (!result.ok) {
        toast.error(result.error);

        return;
      }

      setPendingAdditions([]);
      toast.success("Paklijst bijgewerkt.");
      router.push(`/trips/${trip.id}`);
    });

  return (
    <div className="flex flex-col gap-4">
      {categories.length === 0 ? (
        <Card className="p-8 text-center">
          <p className="text-muted-foreground text-sm">
            Er zijn nog geen categorieën. Maak ze eerst aan in de catalogus.
          </p>
        </Card>
      ) : null}

      <ul className="flex flex-col gap-2">
        {categories.map((category) => {
          const isSelected = selected.has(category.id) || pendingAdditions.includes(category.id);

          return (
            <li key={category.id}>
              <Card className="flex min-h-16 flex-row items-center gap-3 p-3">
                <Checkbox
                  id={`category-${category.id}`}
                  checked={isSelected}
                  onCheckedChange={(checked) => toggle(category.id, checked === true)}
                  className="size-6"
                />
                <Label htmlFor={`category-${category.id}`} className="min-h-11 flex-1 cursor-pointer items-center">
                  {category.name}
                </Label>
              </Card>
            </li>
          );
        })}
      </ul>

      {pendingAdditions.length > 0 ? (
        <Button onClick={addSelected} disabled={pending} className="min-h-12 w-full">
          {pending ? "Bezig..." : `${pendingAdditions.length} categorie(ën) toevoegen`}
        </Button>
      ) : null}

      <AlertDialog open={confirming !== null} onOpenChange={(open) => !open && setConfirming(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirming?.name} van de paklijst halen?</AlertDialogTitle>
            <AlertDialogDescription>
              Er zijn al spullen uit deze categorie klaargelegd, ingepakt of ingeladen. Die worden verwijderd uit
              deze paklijst. Je kunt ze daarna nog terugzetten.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="min-h-11">Annuleren</AlertDialogCancel>
            <AlertDialogAction className="min-h-11" onClick={() => removeCategory(confirming!.categoryId)}>
              Verwijderen
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
