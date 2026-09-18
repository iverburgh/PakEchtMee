"use client";

import { useMemo, useOptimistic, useState, useTransition } from "react";
import Link from "next/link";
import { Check, Layers, RotateCcw, Search, Trash2, Undo2, X } from "lucide-react";
import { toast } from "sonner";
import type { PackingStatus, TripDetails, TripItem } from "@/lib/types";
import { describeAmount, progressStatuses, statusLabels } from "@/lib/types";
import { applyFilter, emptyFilter, isFilterActive, type ItemFilter } from "@/lib/filter";
import {
  advanceTripItemAction,
  deleteTripItemAction,
  restoreTripItemAction,
  revertTripItemAction,
} from "@/lib/actions";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Progress } from "@/components/ui/progress";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";

type StatusPatch = { itemId: string; status: PackingStatus };

const statusTone: Record<PackingStatus, string> = {
  Active: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200",
  Prepared: "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200",
  Packed: "bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-200",
  Loaded: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200",
  Deleted: "bg-muted text-muted-foreground",
};

export function TripWorkspace({ trip }: { trip: TripDetails }) {
  const [filter, setFilter] = useState<ItemFilter>(emptyFilter);
  const [, startTransition] = useTransition();

  // While a change is in flight the item shows its new status but offers no further step,
  // so the client never has to work out the flow itself.
  const [items, patch] = useOptimistic(trip.items, (state: TripItem[], change: StatusPatch) =>
    state.map((item) =>
      item.id === change.itemId
        ? {
            ...item,
            status: change.status,
            nextStatus: null,
            previousStatus: null,
            isDeleted: change.status === "Deleted",
          }
        : item,
    ),
  );

  const visible = useMemo(() => applyFilter(items, filter), [items, filter]);

  const progress = useMemo(() => {
    const live = items.filter((item) => !item.isDeleted);

    return {
      total: live.length,
      loaded: live.filter((item) => item.status === "Loaded").length,
      counts: Object.fromEntries(
        progressStatuses.map((status) => [status, live.filter((item) => item.status === status).length]),
      ) as Record<PackingStatus, number>,
    };
  }, [items]);

  const change = (
    item: TripItem,
    optimisticStatus: PackingStatus,
    action: () => Promise<{ ok: boolean; error?: string }>,
    onSuccess?: () => void,
  ) =>
    startTransition(async () => {
      patch({ itemId: item.id, status: optimisticStatus });

      const result = await action();

      if (!result.ok) {
        toast.error(result.error ?? "De wijziging is niet opgeslagen.");

        return;
      }

      onSuccess?.();
    });

  const advance = (item: TripItem) => {
    const next = item.nextStatus;

    if (next === null) {
      return;
    }

    change(item, next, () => advanceTripItemAction(trip.id, item.id), () =>
      toast.success(`${item.name}: ${statusLabels[next]}`, {
        action: { label: "Ongedaan maken", onClick: () => revert(item) },
      }),
    );
  };

  const revert = (item: TripItem) => {
    const previous = item.previousStatus ?? "Active";

    change(item, previous, () => revertTripItemAction(trip.id, item.id));
  };

  const remove = (item: TripItem) =>
    change(item, "Deleted", () => deleteTripItemAction(trip.id, item.id), () =>
      toast.success(`${item.name} verwijderd.`, {
        action: { label: "Terugzetten", onClick: () => restore(item) },
      }),
    );

  const restore = (item: TripItem) =>
    change(item, item.status, () => restoreTripItemAction(trip.id, item.id));

  const toggleCategory = (categoryIds: string[]) => setFilter((current) => ({ ...current, categoryIds }));
  const toggleStatus = (statuses: string[]) =>
    setFilter((current) => ({ ...current, statuses: statuses as PackingStatus[] }));

  if (trip.items.length === 0) {
    return (
      <main className="px-4">
        <Card className="flex flex-col items-center gap-4 p-8 text-center">
          <Layers className="text-muted-foreground size-10" aria-hidden />
          <p className="text-muted-foreground text-sm">
            Deze paklijst is nog leeg. Kies welke categorieën mee moeten.
          </p>
          <Button render={<Link href={`/trips/${trip.id}/categorieen`} />} nativeButton={false} className="min-h-11">
            Categorieën kiezen
          </Button>
        </Card>
      </main>
    );
  }

  return (
    <main className="flex flex-col gap-4 px-4">
      <section aria-label="Voortgang" className="flex flex-col gap-2">
        <div className="flex items-center gap-3">
          <Progress value={progress.total === 0 ? 0 : (progress.loaded / progress.total) * 100} className="h-2 flex-1" />
          <span className="text-sm font-medium tabular-nums">
            {progress.loaded}/{progress.total} ingeladen
          </span>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {progressStatuses.map((status) => (
            <Badge key={status} variant="secondary" className={statusTone[status]}>
              {statusLabels[status]}: {progress.counts[status]}
            </Badge>
          ))}
        </div>
      </section>

      <section aria-label="Zoeken en filteren" className="flex flex-col gap-3">
        <div className="relative">
          <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2" aria-hidden />
          <Input
            type="search"
            value={filter.query}
            onChange={(event) => setFilter((current) => ({ ...current, query: event.target.value }))}
            placeholder="Zoek in alle categorieën"
            aria-label="Zoek in alle categorieën"
            className="min-h-11 pl-9"
          />
        </div>

        <ToggleGroup
          multiple
          value={filter.categoryIds}
          onValueChange={toggleCategory}
          variant="outline"
          className="flex w-full flex-wrap justify-start gap-1.5"
          aria-label="Filter op categorie"
        >
          {trip.categories.map((category) => (
            <ToggleGroupItem key={category.categoryId} value={category.categoryId} className="min-h-9 rounded-full px-3 text-xs">
              {category.name}
            </ToggleGroupItem>
          ))}
        </ToggleGroup>

        <ToggleGroup
          multiple
          value={filter.statuses}
          onValueChange={toggleStatus}
          variant="outline"
          className="flex w-full flex-wrap justify-start gap-1.5"
          aria-label="Filter op status"
        >
          {[...progressStatuses, "Deleted" as const].map((status) => (
            <ToggleGroupItem key={status} value={status} className="min-h-9 rounded-full px-3 text-xs">
              {statusLabels[status]}
            </ToggleGroupItem>
          ))}
        </ToggleGroup>

        {isFilterActive(filter) ? (
          <div className="flex flex-wrap gap-2">
            <Button variant="ghost" onClick={() => setFilter(emptyFilter)} className="min-h-11">
              <X aria-hidden />
              Filters wissen
            </Button>
            <Button
              render={
                <Link
                  href={`/trips/${trip.id}/overzicht?${new URLSearchParams([
                    ...filter.categoryIds.map((id) => ["categoryId", id] as [string, string]),
                    ...filter.statuses.map((status) => ["status", status] as [string, string]),
                  ]).toString()}`}
                />
              }
              nativeButton={false}
              variant="outline"
              className="min-h-11"
            >
              Overzicht met deze filters
            </Button>
          </div>
        ) : null}
      </section>

      {visible.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 p-8 text-center">
          <p className="text-sm font-medium">Niets gevonden</p>
          <p className="text-muted-foreground text-sm">
            Geen items voor
            {filter.query ? <> zoekterm &ldquo;{filter.query}&rdquo;</> : null}
            {filter.query && (filter.categoryIds.length > 0 || filter.statuses.length > 0) ? " met" : null}
            {filter.categoryIds.length > 0 ? (
              <>
                {" "}
                categorie{filter.categoryIds.length > 1 ? "ën" : ""}{" "}
                {trip.categories
                  .filter((category) => filter.categoryIds.includes(category.categoryId))
                  .map((category) => category.name)
                  .join(", ")}
              </>
            ) : null}
            {filter.statuses.length > 0 ? (
              <> status {filter.statuses.map((status) => statusLabels[status]).join(", ")}</>
            ) : null}
            .
          </p>
          <Button variant="outline" onClick={() => setFilter(emptyFilter)} className="min-h-11">
            Filters wissen
          </Button>
        </Card>
      ) : (
        <ul className="flex flex-col gap-2 pb-4">
          {visible.map((item) => {
            const amount = describeAmount(item.quantity, item.unit);

            return (
              <li key={item.id}>
                <Card className="flex min-h-16 flex-row items-center gap-2 p-2 pl-3">
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate font-medium">
                      {item.name}
                      {amount ? <span className="text-muted-foreground font-normal"> {amount}</span> : null}
                    </span>
                    <span className="text-muted-foreground truncate text-xs">{item.categoryName}</span>
                  </div>

                  <Badge variant="secondary" className={statusTone[item.status]}>
                    {statusLabels[item.status]}
                  </Badge>

                  {item.isDeleted ? (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-11 shrink-0"
                      aria-label={`${item.name} terugzetten`}
                      onClick={() => restore(item)}
                    >
                      <RotateCcw aria-hidden />
                    </Button>
                  ) : (
                    <>
                      {item.previousStatus ? (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="size-11 shrink-0"
                          aria-label={`${item.name} terug naar ${statusLabels[item.previousStatus]}`}
                          onClick={() => revert(item)}
                        >
                          <Undo2 aria-hidden />
                        </Button>
                      ) : (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="size-11 shrink-0"
                          aria-label={`${item.name} verwijderen`}
                          onClick={() => remove(item)}
                        >
                          <Trash2 aria-hidden />
                        </Button>
                      )}

                      {item.nextStatus ? (
                        <Button
                          size="icon"
                          className="size-11 shrink-0"
                          aria-label={`${item.name} naar ${statusLabels[item.nextStatus]}`}
                          onClick={() => advance(item)}
                        >
                          <Check aria-hidden />
                        </Button>
                      ) : (
                        <span className="size-11 shrink-0" aria-hidden />
                      )}
                    </>
                  )}
                </Card>
              </li>
            );
          })}
        </ul>
      )}
    </main>
  );
}
