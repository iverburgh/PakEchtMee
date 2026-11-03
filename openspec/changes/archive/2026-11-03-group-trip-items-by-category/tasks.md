# Tasks

## 1. Grouping helper

- [x] 1.1 Add `components/trips/group-by-category.ts` with `groupByCategory(visible, allItems)` (design D1): groups visible items by `categoryId`, sorted by name with Dutch collation, keeps item order, and computes `loaded`/`total` over the category's non-deleted items in `allItems`. Verify with `npm run lint`, and with a scratch `node --experimental-strip-types` script covering the spec cases: alphabetical order, a filtered-out category has no group, deleted items in their own category, progress independent of filters, and same-named categories with different ids staying separate.

## 2. Grouped walkthrough

- [x] 2.1 Render the visible items in `TripWorkspace` as category sections with a sticky heading button (chevron, name, "x/y ingeladen", `aria-expanded`/`aria-controls`), and remove the grey category line from the item card (design D2, D4). Verify with `npm run lint` and `npm run build`.
- [x] 2.2 Add the collapse state (a `Set` of collapsed category ids, all expanded on open) and the search override (design D3). Verify with `npm run lint` and `npm run build`.
- [x] 2.3 Check every scenario of the four new `trip-packing` requirements in the browser against the Aspire dev environment, at phone and desktop width:
  - headings in alphabetical order;
  - a filtered category with only its own heading;
  - deleted items under their own category;
  - per-category progress, including its update after an advance;
  - the sticky heading while scrolling;
  - collapse and expand;
  - search showing a match in a collapsed category, which collapses again after clearing.
  
  Also check that the existing walkthrough still works: one tap advances, undo works, and the scroll position is kept.

## Workflow follow-up

- After the push to `main`, check the grouped list on production.
- Archive the change with `/opsx:archive`.
