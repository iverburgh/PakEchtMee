export type SelectAllState = {
  /** What the "Alles" checkbox shows: every listed category selected, only some, or none. */
  status: "all" | "some" | "none";
  /** False when every category is already on the trip and nothing is pending: there is nothing to tick or clear. */
  canToggle: boolean;
  /** The pending additions after tapping "Alles". Never contains a category that is already on the trip. */
  nextPending: string[];
};

/**
 * Derives the "Alles" checkbox from the existing selection instead of keeping separate state, so ticking a single
 * category keeps it in sync automatically. Clearing only drops pending additions: categories already on the trip are
 * removed one by one through their own confirmation, so a tap on "Alles" can never lose packing progress.
 */
export function selectAllState(
  categoryIds: readonly string[],
  onTrip: ReadonlySet<string>,
  pending: readonly string[],
): SelectAllState {
  const selectedCount = categoryIds.filter((id) => onTrip.has(id) || pending.includes(id)).length;
  const status = selectedCount === 0 ? "none" : selectedCount === categoryIds.length ? "all" : "some";
  const notOnTrip = categoryIds.filter((id) => !onTrip.has(id));

  return {
    status,
    canToggle: notOnTrip.length > 0,
    nextPending: status === "all" ? [] : notOnTrip,
  };
}
