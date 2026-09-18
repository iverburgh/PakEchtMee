"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ChevronRight, FolderOpen, MoreVertical, Plus, RotateCcw, Trash2, Pencil } from "lucide-react";
import { toast } from "sonner";
import type { Category } from "@/lib/types";
import {
  createCategoryAction,
  removeCategoryAction,
  renameCategoryAction,
  restoreCategoryAction,
} from "@/lib/actions";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { NameDialog } from "@/components/catalog/name-dialog";

export function CategoryList({ categories }: { categories: Category[] }) {
  const [creating, setCreating] = useState(false);
  const [renaming, setRenaming] = useState<Category | null>(null);
  const [, startTransition] = useTransition();
  const router = useRouter();

  const live = categories.filter((category) => !category.isDeleted);
  const removed = categories.filter((category) => category.isDeleted);

  const runAction = (action: () => Promise<{ ok: boolean; error?: string }>, success: string) =>
    startTransition(async () => {
      const result = await action();

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
        Nieuwe categorie
      </Button>

      {live.length === 0 && removed.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 p-8 text-center">
          <FolderOpen className="text-muted-foreground size-10" aria-hidden />
          <p className="text-muted-foreground text-sm">
            Begin met een categorie, bijvoorbeeld &ldquo;Toiletspullen&rdquo;.
          </p>
        </Card>
      ) : null}

      <ul className="flex flex-col gap-2">
        {live.map((category) => (
          <li key={category.id}>
            <Card className="flex min-h-16 flex-row items-center gap-2 p-3">
              <Link href={`/catalogus/${category.id}`} className="flex min-w-0 flex-1 items-center gap-2">
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
                  <DropdownMenuItem onSelect={() => setRenaming(category)}>
                    <Pencil aria-hidden />
                    Hernoemen
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    variant="destructive"
                    onSelect={() => runAction(() => removeCategoryAction(category.id), "Categorie verwijderd.")}
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

      <NameDialog
        open={creating}
        onOpenChange={setCreating}
        title="Nieuwe categorie"
        label="Naam"
        fieldId="new-category-name"
        placeholder="Toiletspullen"
        maxLength={60}
        onSubmit={(name) => createCategoryAction(name)}
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
