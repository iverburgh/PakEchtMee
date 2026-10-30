"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Pencil, Plus, UserMinus, UserPlus, Users } from "lucide-react";
import { toast } from "sonner";
import type { Family } from "@/lib/types";
import {
  addMemberAction,
  createFamilyAction,
  removeMemberAction,
  renameFamilyAction,
  type ActionResult,
} from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
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
import { NameDialog } from "@/components/catalog/name-dialog";

type Removal = { family: Family; email: string };

/** Families and their members. Only the administrator gets here; the API checks that again on every call. */
export function FamilyAdministration({ families }: { families: Family[] }) {
  const [creating, setCreating] = useState(false);
  const [renaming, setRenaming] = useState<Family | null>(null);
  const [addingTo, setAddingTo] = useState<Family | null>(null);
  const [removing, setRemoving] = useState<Removal | null>(null);
  const [, startTransition] = useTransition();
  const router = useRouter();

  const runAction = (action: () => Promise<ActionResult<unknown>>, success: string) =>
    startTransition(async () => {
      const result = await callAction(action);

      if (!result.ok) {
        toast.error(result.error);

        return;
      }

      toast.success(success);
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-4">
      <Button onClick={() => setCreating(true)} className="min-h-11 self-start">
        <Plus aria-hidden />
        Nieuwe familie
      </Button>

      {families.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 p-8 text-center">
          <Users className="text-muted-foreground size-10" aria-hidden />
          <p className="text-muted-foreground text-sm">Nog geen families.</p>
        </Card>
      ) : null}

      <ul className="flex flex-col gap-3">
        {families.map((family) => (
          <li key={family.id}>
            <Card className="flex flex-col gap-3 p-4">
              <div className="flex items-center gap-2">
                <h2 className="min-w-0 flex-1 truncate font-semibold">{family.name}</h2>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-11"
                  aria-label={`${family.name} hernoemen`}
                  onClick={() => setRenaming(family)}
                >
                  <Pencil aria-hidden />
                </Button>
              </div>

              {family.members.length === 0 ? (
                <p className="text-muted-foreground text-sm">Nog geen leden.</p>
              ) : (
                <ul className="flex flex-col divide-y rounded-lg border">
                  {family.members.map((email) => (
                    <li key={email} className="flex min-h-12 items-center gap-2 pl-3">
                      <span className="min-w-0 flex-1 truncate text-sm">{email}</span>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-11"
                        aria-label={`${email} verwijderen uit ${family.name}`}
                        onClick={() => setRemoving({ family, email })}
                      >
                        <UserMinus aria-hidden />
                      </Button>
                    </li>
                  ))}
                </ul>
              )}

              <Button variant="outline" className="min-h-11 self-start" onClick={() => setAddingTo(family)}>
                <UserPlus aria-hidden />
                Lid toevoegen
              </Button>
            </Card>
          </li>
        ))}
      </ul>

      <NameDialog
        open={creating}
        onOpenChange={setCreating}
        title="Nieuwe familie"
        label="Naam"
        fieldId="new-family-name"
        placeholder="Jansen"
        maxLength={60}
        onSubmit={(name) => createFamilyAction(name)}
        successMessage="Familie toegevoegd."
      />

      <NameDialog
        open={renaming !== null}
        onOpenChange={(value) => !value && setRenaming(null)}
        title="Familie hernoemen"
        label="Naam"
        fieldId="rename-family-name"
        maxLength={60}
        initialValue={renaming?.name ?? ""}
        onSubmit={(name) => renameFamilyAction(renaming!.id, name)}
        successMessage="Familie hernoemd."
      />

      <NameDialog
        open={addingTo !== null}
        onOpenChange={(value) => !value && setAddingTo(null)}
        title={addingTo ? `Lid toevoegen aan ${addingTo.name}` : "Lid toevoegen"}
        label="E-mailadres"
        fieldId="new-member-email"
        placeholder="naam@gmail.com"
        maxLength={254}
        onSubmit={(email) => addMemberAction(addingTo!.id, email)}
        successMessage="Lid toegevoegd. Dit account kan nu inloggen."
      />

      <AlertDialog open={removing !== null} onOpenChange={(value) => !value && setRemoving(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Lid verwijderen?</AlertDialogTitle>
            <AlertDialogDescription>
              {removing ? `${removing.email} kan daarna niet meer inloggen. De catalogus en uitjes van ${removing.family.name} blijven bestaan.` : null}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annuleren</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                const target = removing!;
                setRemoving(null);
                runAction(() => removeMemberAction(target.family.id, target.email), "Lid verwijderd.");
              }}
            >
              Verwijderen
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
