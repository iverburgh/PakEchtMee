# Proposal

## Why

The general catalog has 22 categories, so picking most or all of them for a trip means ticking them one by one on the categories page (`/trips/{id}/categorieen`). That is slow, especially on a phone. A single "Alles" (all) checkbox at the top of the list lets a member select every category in one tap. The same checkbox clears that selection again.

## What Changes

- Add an "Alles" checkbox card at the top of the trip category list:
  - **Checked** when every listed category is selected, whether it is already on the trip or ticked but not yet added.
  - **Indeterminate** (a dash) when only some are selected.
  - **Unchecked** when none are selected.
- Tapping it while it is unchecked or indeterminate ticks every category that is not yet on the trip. These categories join the pending selection, so nothing is saved until the member taps the existing "… categorie(ën) toevoegen" button.
- Tapping it while it is checked clears only the pending ticks. Categories already on the trip stay on it. Removing them is still done one by one, with the existing confirmation when items have progressed. This keeps the bulk action from deleting packing progress by accident.
- Hide the "Alles" card when there are no categories, and disable it when every category is already on the trip and nothing is pending (there is nothing to select or clear).
- The shared checkbox component gains a visible indeterminate state (a minus icon).
- No backend or API change. The existing "select categories" endpoint already accepts several category ids at once.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `trip-packing`: adds a requirement for selecting, or clearing the pending selection of, all categories at once on the trip category picker.

## Impact

- **Frontend**
  - `src/web-client/src/components/trips/category-picker.tsx`: the "Alles" card and the bulk select/clear logic.
  - `src/web-client/src/components/ui/checkbox.tsx`: render a minus icon for the indeterminate state (Base UI `indeterminate` prop).
  - A small pure helper for the selection state (all / some / none), colocated with the picker.
- **Backend**: none.
- **Dependencies**: none.
