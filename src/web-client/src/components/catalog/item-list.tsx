"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { FolderInput, MoreVertical, PackageOpen, Pencil, Plus, RotateCcw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { CatalogItem, Category } from "@/lib/types";
import { describeAmount } from "@/lib/types";
import {
  createItemAction,
  moveItemAction,
  removeItemAction,
  restoreItemAction,
  updateItemAction,
} from "@/lib/actions";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ItemDialog } from "@/components/catalog/item-dialog";
import { MoveItemDialog } from "@/components/catalog/move-item-dialog";

export function ItemList({
  categoryId,
  items,
  categories,
}: {
  categoryId: string;
  items: CatalogItem[];
  categories: Category[];
}) {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<CatalogItem | null>(null);
  const [moving, setMoving] = useState<CatalogItem | null>(null);
  const [, startTransition] = useTransition();
  const router = useRouter();

  const live = items.filter((item) => !item.isDeleted);
  const removed = items.filter((item) => item.isDeleted);

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
        Nieuw item
      </Button>

      {live.length === 0 && removed.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 p-8 text-center">
          <PackageOpen className="text-muted-foreground size-10" aria-hidden />
          <p className="text-muted-foreground text-sm">Nog geen spullen in deze categorie.</p>
        </Card>
      ) : null}

      <ul className="flex flex-col gap-2">
        {live.map((item) => {
          const amount = describeAmount(item.quantity, item.unit);

          return (
            <li key={item.id}>
              <Card className="flex min-h-16 flex-row items-center gap-2 p-3">
                <span className="min-w-0 flex-1 truncate">
                  {item.name}
                  {amount ? <span className="text-muted-foreground"> {amount}</span> : null}
                </span>
                <DropdownMenu>
                  <DropdownMenuTrigger
                    render={
                      <Button variant="ghost" size="icon" className="size-11" aria-label={`Acties voor ${item.name}`} />
                    }
                  >
                    <MoreVertical aria-hidden />
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onSelect={() => setEditing(item)}>
                      <Pencil aria-hidden />
                      Bewerken
                    </DropdownMenuItem>
                    <DropdownMenuItem onSelect={() => setMoving(item)}>
                      <FolderInput aria-hidden />
                      Verplaatsen
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      variant="destructive"
                      onSelect={() => runAction(() => removeItemAction(categoryId, item.id), "Item verwijderd.")}
                    >
                      <Trash2 aria-hidden />
                      Verwijderen
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </Card>
            </li>
          );
        })}
      </ul>

      {removed.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h2 className="text-muted-foreground px-1 text-sm font-medium">Verwijderd</h2>
          <ul className="flex flex-col gap-2">
            {removed.map((item) => (
              <li key={item.id}>
                <Card className="flex min-h-16 flex-row items-center gap-2 p-3">
                  <span className="text-muted-foreground min-w-0 flex-1 truncate line-through">{item.name}</span>
                  <Badge variant="secondary">Verwijderd</Badge>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-11"
                    aria-label={`${item.name} terugzetten`}
                    onClick={() => runAction(() => restoreItemAction(categoryId, item.id), "Item teruggezet.")}
                  >
                    <RotateCcw aria-hidden />
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <ItemDialog
        open={creating}
        onOpenChange={setCreating}
        title="Nieuw item"
        onSubmit={(values) => createItemAction({ categoryId, ...values })}
        successMessage="Item toegevoegd."
      />

      <ItemDialog
        open={editing !== null}
        onOpenChange={(value) => !value && setEditing(null)}
        title="Item bewerken"
        initialValue={editing ?? undefined}
        onSubmit={(values) => updateItemAction(categoryId, editing!.id, values)}
        successMessage="Item bijgewerkt."
      />

      <MoveItemDialog
        item={moving}
        categories={categories.filter((candidate) => candidate.id !== categoryId)}
        onOpenChange={(value) => !value && setMoving(null)}
        onSubmit={(targetCategoryId) => moveItemAction(categoryId, moving!.id, targetCategoryId)}
      />
    </div>
  );
}
