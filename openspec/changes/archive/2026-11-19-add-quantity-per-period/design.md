# Design

## Context

See proposal.md (Why). This builds directly on the quantity rule from the archived change `add-quantity-per-person`:

- **Amount types.** Catalog and Trips each have their own `Amount` (`Quantity`, `Unit`, `Basis`). It is persisted as an optional owned type, so every column of it is nullable and a value column is back-filled only where a quantity exists.
- **Totals.** `TripItem.Total` is stored, not derived:
  - It is computed by `Amount.TotalFor(TripComposition)` when an item is created and when its rule changes.
  - `TripItem.FollowComposition` recomputes it for `Active` items when `Trip.ChangeComposition` runs.
  - Items that have progressed keep their total.
- **Trip dates.** `TripDates(Start?, End?)` is set only at creation; nothing can change it afterwards. The seeded trips have no dates.
- **Composition endpoint.** `PUT /api/trips/{id}/composition` → `ChangeTripCompositionHandler` → `TripCompositionChangedDto(Trip, ChangedTotals)`. The only caller is the trip header's `CompositionDialog`.
- **Seed rules.** `CatalogSeedData.PerHead` maps `(category, item)` to a `QuantityBasis`.
  - The `AddQuantityBasis` migration test compares its literal GUID lists against that map.
  - `Kleding/Onderbroeken`, `Kleding/Sokken` and `Kinderen – Kleding/Sokken` are seeded without a quantity.
- **Frontend.**
  - `QuantityFields` holds quantity, unit and basis, and is shared by the catalog item dialog and the trip item quantity dialog.
  - `describeRule` and `RuleChip` render the rule.
  - `CompositionForm` is the − / + stepper form used by the Gezin page and the trip dialog.

## Goals / Non-Goals

**Goals:**

- One rule model: quantity × basis × period, in both contexts, with each context keeping its own types as before.
- Changing who and when is one atomic step with one recalculation and one message, not two calls that can half-succeed.
- Every existing rule keeps its meaning: per trip is the default everywhere, including in the API.

**Non-Goals:**

- No "per N days" rules, no maximum ("wash halfway"), no rounding.
- No editing of the trip name in the new dialog. Only who and when.
- The Gezin page gets no period or duration settings, because a family has no duration.

## Decisions

### D1. `QuantityPeriod` extends `Amount` in both contexts, like `QuantityBasis`

Each context gets `enum QuantityPeriod { PerTrip, PerDay, PerNight }` with `QuantityPeriods.Parse(string?)` (a missing value means `PerTrip`, an unknown one gives a validation error) and a Dutch suffix ("", "per dag", "per nacht").

- `Amount.Create(quantity, unit, basis = Fixed, period = PerTrip)`.
- A period other than `PerTrip` without a quantity is rejected, the same rule as for the basis.
- `ToString()` joins the parts: "1 per kind per dag", "2 per nacht", "2 liter per persoon per dag".
- The period is persisted as `QuantityPeriod nvarchar(20) NULL` next to `QuantityBasis`, for the same reason that column is nullable.
- `CatalogItemSnapshot` gains `QuantityPeriod Period = PerTrip`.

**Alternative:** a third value in `QuantityBasis` such as "per person per day". Rejected, because the two dimensions combine freely (fixed per night, per child per day), and a single enum would need every combination.

### D2. `TripDates` knows its days and nights

- `TripDates.Days`: `End - Start + 1` when both dates are set, otherwise null.
- `TripDates.Nights`: `Days - 1` when both dates are set, otherwise null.
- `TripDates.Multiplier(QuantityPeriod)`:
  - `PerTrip` → 1;
  - `PerDay` → `Days ?? 1`;
  - `PerNight` → `Nights ?? 1`.
- `Amount.TotalFor(TripComposition, TripDates)` = `Quantity × composition.Multiplier(Basis) × dates.Multiplier(Period)`. The old single-argument overload goes.

**Why count once without dates:** this was the user's choice. The total stays meaningful as a minimum, and the UI says the dates are missing (D7).

### D3. `Trip.ChangePlan` replaces `Trip.ChangeComposition`

`Trip.ChangePlan(TripComposition composition, TripDates dates)` sets both and returns how many totals changed.

- `TripItem.FollowComposition` becomes `TripItem.FollowPlan(composition, dates)`.
- It recomputes only non-deleted `Active` items whose rule depends on who or when: `Basis != Fixed || Period != PerTrip`.
- `TripItem.Create` and `ChangeAmount` take the dates too.
- `Trip.SelectCategories` and `ChangeItemAmount` pass `Dates`.

**Why one method:** the dialog changes both at once. A single aggregate operation gives one consistent recalculation and one count for the toast.

### D4. `PUT /api/trips/{id}/plan` replaces the composition endpoint

- **Request:** `{ adults, children, startDate, endDate }`.
- **Handler:** `ChangeTripPlanHandler`, which uses the existing `TripComposition.Create` and `TripDates.Create` validation.
- **Response:** `TripPlanChangedDto(Trip, ChangedTotals)`. The handler and DTO for the composition are renamed, not duplicated.
- **Trip details:** `TripDetailsDto` gains `Days` and `Nights` (null without dates), so the client can show the duration and decide on the no-dates hint without computing dates itself.

**Why replace instead of keep both:** the web client is the only consumer and is deployed together with the API. Two endpoints that both recalculate would be two ways to get the same thing slightly wrong.

### D5. API payloads carry `period`

- Catalog `POST/PUT /api/items` and trip item `PUT …/quantity` accept an optional `period`. A missing value means `PerTrip`.
- Every catalog item and trip item DTO returns `period` (`"PerTrip"` without a quantity).
- `CategorySelections` parses it into the snapshot, like the basis.

### D6. Seed data and migrations

- **Seed rule type:** `CatalogSeedData.PerHead` becomes `IReadOnlyDictionary<(string Category, string Item), SeedRule>`, with `record SeedRule(QuantityBasis Basis, QuantityPeriod Period = PerTrip)`.
  - The three clothing items get `(PerAdult, PerDay)` and `(PerChild, PerDay)`.
  - `SeedItem` gains `Period`, and `CatalogSeeder` passes it on.
  - The existing `AddQuantityBasis` migration test filters on `Period == PerTrip`, so its comparison stays exact.
- **Catalog `AddQuantityPeriod`:**
  - Adds the column, then sets it to `'PerTrip'` where a quantity exists.
  - A data step sets `Quantity = 1`, the basis and `QuantityPeriod = 'PerDay'` for the three seeded ids (literal GUIDs). It only applies `WHERE` the category is general `AND Quantity IS NULL AND Unit IS NULL`.
- **Trips `AddQuantityPeriod`:** adds the column, then sets it to `'PerTrip'` where a quantity exists. Existing totals are already correct, because every existing rule is per trip.

### D7. Frontend

- **`QuantityFields`:** gets a second single-choice toggle group "Hoe vaak" with Per uitje / Per dag / Per nacht under "Telt voor".
  - The zod rule rejects a period without a quantity.
  - `fromQuantityValues` drops the period back to `PerTrip` when the quantity is empty, like the basis.
- **`lib/types.ts`:** `QuantityPeriod` type with labels and suffixes. `describeRule(quantity, unit, basis, period)` appends the period suffix.
- **Rule display:**
  - `RuleChip` shows the chip for any rule that is not fixed per trip. A fixed-per-night rule gets a neutral chip, because there is no basis colour.
  - `QuantityBadge` takes `missingDates`. When it is true and the period is not per trip, a small calendar icon with "geen datums" text follows the chip.
  - The trip workspace passes `trip.days === null`.
- **Trip header:**
  - The subline (date range plus "2 volwassenen · 3 kinderen") becomes the trigger of `PlanDialog` ("Wie en wanneer?"). It holds the steppers from `CompositionForm` and two date inputs.
  - `CompositionForm` gets an optional dates section, rather than a second form, so the Gezin page keeps using it without dates.
  - The toast reports the number of changed totals, as now.
- **Overview:** shows the rule text with its period, and the duration ("15 dagen · 14 nachten") next to the composition.

## Risks / Trade-offs

- [Counting once without dates gives a deceptively low total] → The hint is shown on every such item, and the dialog to add dates is one tap away in the header.
- [The plan endpoint replaces the composition endpoint] → The client and API ship in one deploy, and the Web tests cover the new endpoint and the 404 for a foreign trip.
- [Changing dates reorders the trip list] → That is intended ("most relevant first"). No extra handling.
- [Day trips have 0 nights, so per-night items total 0] → This is consistent with the spec, and the item stays visible so the member can change or delete it.

## Migration Plan

1. Deploy as usual. The two migrations run at API startup.
2. **Rollback:** revert and redeploy. The new nullable columns are ignored by the previous code. The data step only gave three general items a per-day rule. After a rollback, that rule reads as per trip, and an administrator can adjust the items.
