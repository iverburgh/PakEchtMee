"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import type { ActionResult } from "@/lib/actions";
import type { CatalogItem, Category } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function MoveItemDialog({
  item,
  categories,
  onOpenChange,
  onSubmit,
}: {
  item: CatalogItem | null;
  categories: Category[];
  onOpenChange: (open: boolean) => void;
  onSubmit: (targetCategoryId: string) => Promise<ActionResult<unknown>>;
}) {
  const [target, setTarget] = useState<string>("");
  const [pending, startTransition] = useTransition();
  const router = useRouter();

  const submit = () =>
    startTransition(async () => {
      const result = await onSubmit(target);

      if (!result.ok) {
        toast.error(result.error);

        return;
      }

      toast.success("Item verplaatst.");
      setTarget("");
      onOpenChange(false);
      router.refresh();
    });

  return (
    <Dialog open={item !== null} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{item?.name} verplaatsen</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-2">
          <Label htmlFor="target-category">Naar categorie</Label>
          <Select value={target} onValueChange={(value) => setTarget(value ?? "")}>
            <SelectTrigger id="target-category" className="min-h-11 w-full">
              <SelectValue placeholder="Kies een categorie" />
            </SelectTrigger>
            <SelectContent>
              {categories.map((category) => (
                <SelectItem key={category.id} value={category.id}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <DialogFooter>
          <Button onClick={submit} disabled={pending || target === ""} className="min-h-11 w-full">
            {pending ? "Bezig..." : "Verplaatsen"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
