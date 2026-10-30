"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ChevronRight, FolderOpen, Globe, MoreVertical, Plus, RotateCcw, Trash2, Pencil } from "lucide-react";
import { toast } from "sonner";
import type { Category } from "@/lib/types";
import {
  createCategoryAction,
  removeCategoryAction,
  renameCategoryAction,
  restoreCategoryAction,
  switchCategoryAction,
  type ActionResult,
} from "@/lib/actions";
import { callAction } from "@/lib/call-action";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { NameDialog } from "@/components/catalog/name-dialog";

/**
 * `family`: the family's own categories (editable) plus the general ones, read-only with an on/off switch.
 * `general`: the administrator's view of the general catalog, editable like own categories.
 */
export type CatalogScope = "family" | "general";

export function CategoryList({ categories, scope = "family" }: { categories: Category[]; scope?: CatalogScope }) {
  const [creating, setCreating] = useState(false);
  const [renaming, setRenaming] = useState<Category | null>(null);
  const [, startTransition] = useTransition();
  const router = useRouter();

  const editable = categories.filter((category) => scope === "general" || !category.isGeneral);
  const live = editable.filter((category) => !category.isDeleted);
  const removed = editable.filter((category) => category.isDeleted);
  const shared = scope === "family" ? categories.filter((category) => category.isGeneral && !category.isDeleted) : [];
  const basePath = scope === "general" ? "/beheer/catalogus" : "/catalogus";

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
        {scope === "general" ? "Nieuwe algemene categorie" : "Nieuwe categorie"}
      </Button>

      {live.length === 0 && removed.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 p-8 text-center">
          <FolderOpen className="text-muted-foreground size-10" aria-hidden />
          <p className="text-muted-foreground text-sm">
            {scope === "general"
              ? "Nog geen algemene categorieën. Wat je hier maakt, zien alle families."
              : "Nog geen eigen categorieën. Maak er een voor spullen die alleen jullie meenemen."}
          </p>
        </Card>
      ) : null}

      <ul className="flex flex-col gap-2">
        {live.map((category) => (
          <li key={category.id}>
            <Card className="flex min-h-16 flex-row items-center gap-2 p-3">
              <Link href={`${basePath}/${category.id}`} className="flex min-w-0 flex-1 items-center gap-2">
                <span className="truncate font-medium">{category.name}</span>
                <ChevronRight className="text-muted-foreground ml-auto size-4 shrink-0" aria-hidden />
              </Link>
              <DropdownMenu>
                <DropdownMenuTrigger
                  render={
                    <Button variant="ghost" size="icon" className="size-11" aria-label={`Acties voor ${category.name}`} />
                  }
                >
                  <MoreVertical aria-hidden />
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => setRenaming(category)}>
                    <Pencil aria-hidden />
                    Hernoemen
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    variant="destructive"
                    onClick={() => runAction(() => removeCategoryAction(category.id), "Categorie verwijderd.")}
                  >
                    <Trash2 aria-hidden />
                    Verwijderen
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </Card>
          </li>
        ))}
      </ul>

      {removed.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h2 className="text-muted-foreground px-1 text-sm font-medium">Verwijderd</h2>
          <ul className="flex flex-col gap-2">
            {removed.map((category) => (
              <li key={category.id}>
                <Card className="flex min-h-16 flex-row items-center gap-2 p-3">
                  <span className="text-muted-foreground min-w-0 flex-1 truncate line-through">{category.name}</span>
                  <Badge variant="secondary">Verwijderd</Badge>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-11"
                    aria-label={`${category.name} terugzetten`}
                    onClick={() => runAction(() => restoreCategoryAction(category.id), "Categorie teruggezet.")}
                  >
                    <RotateCcw aria-hidden />
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {shared.length > 0 ? (
        <section className="flex flex-col gap-2 pt-2">
          <div className="flex flex-col gap-0.5 px-1">
            <h2 className="flex items-center gap-1.5 text-sm font-medium">
              <Globe className="size-4" aria-hidden />
              Algemene categorieën
            </h2>
            <p className="text-muted-foreground text-xs">
              Beheerd door de beheerder. Zet uit wat jullie nooit meenemen; uitgezette categorieën verschijnen niet bij
              nieuwe uitjes.
            </p>
          </div>
          <ul className="flex flex-col gap-2">
            {shared.map((category) => (
              <li key={category.id}>
                <Card
                  className={`flex min-h-16 flex-row items-center gap-3 p-3 transition-opacity motion-reduce:transition-none ${category.isSwitchedOn ? "" : "opacity-60"}`}
                >
                  <Checkbox
                    checked={category.isSwitchedOn}
                    aria-label={`${category.name} gebruiken`}
                    className="size-5"
                    onCheckedChange={(checked) =>
                      runAction(
                        () => switchCategoryAction(category.id, checked === true),
                        checked === true ? `${category.name} staat aan.` : `${category.name} staat uit.`,
                      )
                    }
                  />
                  <Link href={`/catalogus/${category.id}`} className="flex min-w-0 flex-1 items-center gap-2">
                    <span className="truncate font-medium">{category.name}</span>
                    <Badge variant="secondary" className="shrink-0">
                      Algemeen
                    </Badge>
                    <ChevronRight className="text-muted-foreground ml-auto size-4 shrink-0" aria-hidden />
                  </Link>
                </Card>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <NameDialog
        open={creating}
        onOpenChange={setCreating}
        title={scope === "general" ? "Nieuwe algemene categorie" : "Nieuwe categorie"}
        label="Naam"
        fieldId="new-category-name"
        placeholder="Toiletspullen"
        maxLength={60}
        onSubmit={(name) => createCategoryAction(name, scope === "general")}
        successMessage="Categorie toegevoegd."
      />

      <NameDialog
        open={renaming !== null}
        onOpenChange={(value) => !value && setRenaming(null)}
        title="Categorie hernoemen"
        label="Naam"
        fieldId="rename-category-name"
        maxLength={60}
        initialValue={renaming?.name ?? ""}
        onSubmit={(name) => renameCategoryAction(renaming!.id, name)}
        successMessage="Categorie hernoemd."
      />
    </div>
  );
}
