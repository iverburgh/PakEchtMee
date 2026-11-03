# Tasks

## 1. Indeterminate checkbox

- [x] 1.1 Let `components/ui/checkbox.tsx` show a `MinusIcon` and the primary background when the checkbox is indeterminate (design D4). Verify with `npm run lint` and `npm run build`, and check that the existing checkboxes on the trip category page and in catalog management look unchanged.

## 2. "Alles" control in the category picker

- [x] 2.1 Add the pure helper `components/trips/select-all.ts` (`all` / `some` / `none`, plus whether it can be toggled, and the pending ids for the next tap; design D2). Verify the cases from the spec by reading them through in the picker during 2.3, and with `npm run lint`.
- [x] 2.2 Render the "Alles" card at the top of `CategoryPicker`, wired to the helper. Hide it when there are no categories, and disable it when every category is on the trip and nothing is pending (design D1, D3). Verify with `npm run lint` and `npm run build`.
- [x] 2.3 Check every scenario of both new `trip-packing` requirements in the browser against the Aspire dev environment, at phone width and desktop width:
  - select all on an empty trip;
  - leave the page without confirming;
  - complete a partial selection;
  - clear pending ticks;
  - clearing keeps a category that has packed items;
  - everything already on the trip;
  - no categories.

## Workflow follow-up

- After the push to `main`, check the "Alles" control on production.
- Archive the change with `/opsx:archive`.
