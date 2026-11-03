# Design

## Context

`TripWorkspace` (`src/web-client/src/components/trips/trip-workspace.tsx`) is a client component. It holds the trip items in `useOptimistic`, derives `visible` with `applyFilter`, and renders `visible` as one flat `<ul>` of item cards. Each card shows `item.categoryName` as a grey line.

Every `TripItem` carries `categoryId` and `categoryName` (a snapshot). A family can have an own category and a general category with the same name, so the name alone is not unique.

The overview page (`overzicht/page.tsx`) has its own `group()` that groups by `categoryName`. The page header is not sticky, and the bottom navigation is fixed, so a sticky element at `top-0` does not collide with anything.

The web client has no unit-test runner. Its checks are `npm run lint` and `npm run build`, which CI runs.

## Goals / Non-Goals

**Goals:**

- Grouping, per-category progress and collapse all derive from the existing optimistic `items`, so an advance or undo updates the heading in the same render as the item.
- Keep the walkthrough guarantees in `trip-packing`: one tap to advance, undo, and keeping the scroll position.

**Non-Goals:**

- No change to the overview or print page. Its own name-based grouping stays as it is.
- Collapse state is not remembered across page loads.
- No new UI primitive such as an accordion or collapsible.

## Decisions

### D1. A pure helper groups by `categoryId`

`groupByCategory(visible, allItems)` lives in `components/trips/group-by-category.ts` and returns an ordered list of `{ categoryId, name, items, loaded, total }`.

- **Grouping key**: `categoryId`, because names are not unique (an own category and a general one can share a name). The heading shows the item's `categoryName`.
- **Order**: by name with `localeCompare(…, "nl")`, the same as the overview. Within a group, items keep their existing order.
- **Progress**: `loaded` and `total` come from all non-deleted items of the category in `allItems`, not from `visible`. That way a filter does not change the category's progress, matching the spec and the trip-level progress.
- **Alternative**: reuse the overview's `group()`. Rejected, because it groups by name and has no progress.

### D2. The heading is a sticky `<button>` with `aria-expanded`

Each group is a `<section>`. Its heading is a full-width `<button>` with:

- `sticky top-0 z-10` and a solid background (`bg-background/95` with `backdrop-blur`), so cards scroll underneath without showing through;
- a chevron, the category name, and the progress "x/y ingeladen";
- a minimum height of 44 px;
- `aria-expanded` and `aria-controls` pointing at the group's `<ul>`.

Because the sticky element sits inside its own section, the next section's heading naturally pushes it out of view.

- **Why**: a native button gives keyboard and screen-reader support for free, without a new dependency.
- **Alternative**: Base UI Collapsible or a shadcn accordion. That is more code and a new component for a simple show/hide. An accordion would also force one-open-at-a-time or extra configuration.

### D3. Collapse state is a `Set<string>` of collapsed category ids, overridden while searching

The workspace keeps `collapsed` in `useState`. When rendering, a group is expanded if `filter.query.trim() !== ""` or its id is not in `collapsed`. The search override only happens at render time and never writes to `collapsed`, so clearing the query brings back what the member chose. Category and status filters do not override collapse, because those headings show their progress and the member chose to hide them.

### D4. The item card loses its category line

The card keeps its name, amount, status badge and actions. Only the `categoryName` line goes. The cards may get slightly shorter, but they keep `min-h-16` so the touch targets stay large.

## Risks / Trade-offs

- [No automated frontend test] → The helper is pure. Its spec cases are checked with a scratch `node --experimental-strip-types` script during implementation, the same as the earlier select-all change, plus lint and build in CI and a manual check in the browser. Adding Vitest stays a separate, later decision.
- [A collapsed category hides items that still need packing] → The heading keeps showing "x/y ingeladen", so open work is still visible, and one tap expands it again.
- [Sticky headings plus the fixed bottom navigation take up screen space on small phones] → The heading is a single 44 px row. It is accepted for the clearer structure.
