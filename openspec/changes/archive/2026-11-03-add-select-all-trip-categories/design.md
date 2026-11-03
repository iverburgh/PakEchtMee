# Design

## Context

`CategoryPicker` (`src/web-client/src/components/trips/category-picker.tsx`) is a client component with two kinds of selection:

- **On the trip**: derived from `trip.categories`. Unticking one of these calls `removeTripCategoryAction` straight away, and first shows a confirmation dialog when the category has progressed items.
- **Pending additions**: the `pendingAdditions` state. These are only saved when the member taps "… categorie(ën) toevoegen", which calls `selectTripCategoriesAction` with all pending ids at once.

`categories` is the list the API returns for the family, already limited to categories that can be selected. The shared `Checkbox` (`components/ui/checkbox.tsx`) wraps the Base UI checkbox. Base UI supports an `indeterminate` prop and sets `data-indeterminate`, but the wrapper only renders a check icon. The web client has no unit-test runner; its checks are `npm run lint` and `npm run build`, which CI runs.

## Goals / Non-Goals

**Goals:**

- The "Alles" control works entirely on the existing client state, so the change stays frontend-only.
- The state logic (checked / partly / none, what a tap does) is a small pure function, so it is easy to read and to test later.

**Non-Goals:**

- No bulk removal of categories from the trip (the user chose this explicitly). Removing stays one category at a time, with its confirmation.
- No select-all in the catalog management screens (`catalog/category-list.tsx`).
- No new frontend test framework.

## Decisions

### D1. "Alles" is a card at the top of the list, styled like the category cards

It is the same `Card` + `Checkbox` + `Label` row with a 44 px minimum touch target. A thin divider or extra gap separates it from the categories.

- **Why**: it reads as part of the list, works with one thumb on a phone, and the indeterminate state shows at a glance how much is already selected.
- **Alternatives**: two buttons ("Alles selecteren" / "Alles deselecteren") or one toggle button. The user preferred the checkbox.

### D2. The state is derived, not stored

A pure helper `selectAllState(categoryIds, onTrip, pending)` is colocated with the picker in `components/trips/select-all.ts`.

- It returns `"all" | "some" | "none"`, plus `canToggle`. `canToggle` is false when every category is on the trip and nothing is pending.
- A tap does one of two things:
  - from `"none"` or `"some"`, it sets `pendingAdditions` to every category id that is not on the trip;
  - from `"all"`, it sets `pendingAdditions` to `[]`.
- **Why**: there is no extra state that can drift from the individual checkboxes, so ticking or unticking one category updates "Alles" automatically.
- **Alternative**: a separate `allSelected` boolean in state. Rejected, because it would need syncing on every single toggle.

### D3. Clearing never calls the API

Clearing only empties `pendingAdditions`. It never calls `removeTripCategoryAction` and never opens the confirmation dialog. This follows from the spec, and it means an accidental tap can never lose packing progress.

### D4. The indeterminate state is added to the shared `Checkbox`

The `Indicator` renders a `MinusIcon` (lucide) when the root has `data-indeterminate`, and a `CheckIcon` otherwise. The root also gets the primary background for `data-indeterminate`.

- **Why**: the indeterminate look is reusable and belongs to the shadcn-style primitive, not to the picker.
- Existing usages pass no `indeterminate`, so they do not change.

## Risks / Trade-offs

- [No automated frontend test] → The helper is pure and small. The task list verifies it through lint and build in CI, plus a manual check of every spec scenario in the browser (dev server, phone width). Adding Vitest just for this would be a new dependency for one function. It is a reasonable follow-up when the client gets more logic.
- [Selecting all adds many items in one request (22 categories ≈ 150 trip items)] → The existing endpoint already handles several categories per request, and the seeded camping trip has the same size. No change is needed.
- [A category is added or removed by another family member while the page is open] → The page already works on a server-rendered snapshot. "Alles" uses the same snapshot, and the API ignores or deduplicates as it does today.
