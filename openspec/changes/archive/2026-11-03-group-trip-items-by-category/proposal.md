# Proposal

## Why

The packing list on a trip page (`/trips/{id}`) is one long, flat list. The category is only shown as a small grey line under each item. On a trip with 150 items it is hard to see where one category ends and the next begins, so you cannot pack per category (for example, "everything for the kitchen box now"). A clear heading per category, with that category's items below it, matches how the family actually packs.

## What Changes

- The trip's packing list is grouped per category. Each group has a heading with the category name, and categories are sorted alphabetically, like the overview.
- Each heading shows that category's progress as "loaded / total", counting the non-deleted items in the category. It updates immediately after every status change, just like the trip progress.
- The heading of the category you are in stays at the top of the screen while you scroll through its items (sticky).
- Tapping a heading collapses or expands that category. All categories start expanded. A search query temporarily shows every matching category expanded, so a search never hides a match.
- The grey category line under each item disappears, because the heading already says which category it is.
- Search and filters keep working as before. A category with no visible items after filtering gets no heading.
- No backend or API change: every trip item already carries its category id and name.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `trip-packing`: adds a requirement for showing the walkthrough grouped per category, with a sticky, collapsible heading that shows the category's progress.

## Impact

- **Frontend**
  - `src/web-client/src/components/trips/trip-workspace.tsx`: renders category groups instead of one flat list.
  - A small pure helper that groups the visible items and computes the progress per category, colocated with the workspace.
- **Unchanged**: the overview and print page (`/trips/{id}/overzicht`), the item actions (advance, undo, delete, restore), and the filters.
- **Backend**: none.
- **Dependencies**: none.
