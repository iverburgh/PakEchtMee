import type { TripItem } from "@/lib/types";

export type CategoryGroup = {
  categoryId: string;
  name: string;
  /** The visible items of this category, in their original order. */
  items: TripItem[];
  /** Non-deleted items of the whole category that reached `Loaded`, regardless of search and filters. */
  loaded: number;
  /** Non-deleted items of the whole category, regardless of search and filters. */
  total: number;
};

/**
 * Groups the visible items per category for the walkthrough. Groups are keyed by category id because an own
 * category and a general one can share a name; they are ordered by name like the overview. Progress is counted over
 * `allItems` so a filter never changes what a heading reports.
 */
export function groupByCategory(visible: readonly TripItem[], allItems: readonly TripItem[]): CategoryGroup[] {
  const groups = new Map<string, CategoryGroup>();

  for (const item of visible) {
    const group = groups.get(item.categoryId);

    if (group) {
      group.items.push(item);
    } else {
      groups.set(item.categoryId, { categoryId: item.categoryId, name: item.categoryName, items: [item], loaded: 0, total: 0 });
    }
  }

  for (const item of allItems) {
    const group = groups.get(item.categoryId);

    if (!group || item.isDeleted) {
      continue;
    }

    group.total++;

    if (item.status === "Loaded") {
      group.loaded++;
    }
  }

  return [...groups.values()].sort((left, right) => left.name.localeCompare(right.name, "nl"));
}
