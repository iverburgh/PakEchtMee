import type { PackingStatus, TripItem } from "@/lib/types";

export function normalize(value: string): string {
  return value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase();
}

export type ItemFilter = {
  query: string;
  categoryIds: string[];
  statuses: PackingStatus[];
};

export const emptyFilter: ItemFilter = { query: "", categoryIds: [], statuses: [] };

export function isFilterActive(filter: ItemFilter): boolean {
  return filter.query !== "" || filter.categoryIds.length > 0 || filter.statuses.length > 0;
}

/**
 * Values within one filter kind are a union, the kinds intersect, and deleted items stay hidden
 * unless they are explicitly asked for.
 */
export function applyFilter(items: TripItem[], filter: ItemFilter): TripItem[] {
  const showDeleted = filter.statuses.includes("Deleted");
  const needle = normalize(filter.query.trim());

  return items.filter((item) => {
    if (item.isDeleted && !showDeleted) {
      return false;
    }

    if (filter.categoryIds.length > 0 && !filter.categoryIds.includes(item.categoryId)) {
      return false;
    }

    if (filter.statuses.length > 0 && !filter.statuses.includes(item.status)) {
      return false;
    }

    return needle === "" || normalize(item.name).includes(needle);
  });
}
