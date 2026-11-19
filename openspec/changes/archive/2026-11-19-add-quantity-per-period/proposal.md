# Proposal

## Why

A quantity rule can now say how many to pack per person, adult or child, but not for how long. Socks and underwear depend on the length of the trip: "1 per person" is right for a weekend and far too few for two weeks. The family wants to express "1 per kind per dag" or "1 per nacht" and have the trip work out the total from its dates.

## What Changes

- **Period on a quantity rule**: next to the basis (fixed, per person, per adult, per child), a rule gets an optional period:
  - per uitje (once, the default and today's behaviour);
  - per dag;
  - per nacht.
  
  The rule then reads, for example, "1 per kind per dag" or "2 per nacht".
- **Trip duration**: a trip counts its days and nights from its dates. A trip from 4 to 18 July has 15 days and 14 nights.
- **Totals**: the total becomes quantity × heads × periods. For example, socks at 1 per child per day for 3 children over 15 days gives 45.
- **Trip without dates**: a per-day or per-night item counts as 1 day or 1 night. It shows a hint that the trip has no dates yet.
- **Change a trip's dates**: the "Wie gaan er mee?" dialog on a trip becomes "Wie en wanneer?". In it a member can also change the start and end date afterwards, with the same rules as when creating a trip.
  - A change recalculates the totals of the items still in Te doen whose rule depends on the composition or the duration.
  - Items that have progressed keep their total, as with the composition today.
- **Change on a trip item**: the quantity dialog on a trip item gets the same period choice.
- **Seeded catalog**: `Kleding` → `Onderbroeken` and `Sokken` become 1 per adult per day, and `Kinderen – Kleding` → `Sokken` becomes 1 per child per day.
  - A one-off migration updates these three general items in existing databases, but only while they still have no amount.
- **API**:
  - Catalog and trip item payloads gain `period` next to `basis`.
  - Trip details gain `days` and `nights`, which are null without dates.
  - **BREAKING (internal)**: `PUT /api/trips/{id}/composition` is replaced by `PUT /api/trips/{id}/plan` with `adults`, `children`, `startDate` and `endDate`, so composition and dates change in one step. The web client is the only consumer.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `packing-catalog`: "Optional quantity and unit on a catalog item" gains the period.
- `trip-packing`:
  - The total includes the period.
  - "Change a trip's composition" becomes "Change who and when for a trip", covering both composition and dates.
  - Changing a trip item's quantity includes the period.
  - The rule display shows the period and the no-dates hint.
  - Added: the trip duration in days and nights.
- `development-seed-data`:
  - The "only these items carry a quantity" rule includes the new per-day clothing.
  - Added: the per-day seed items and their one-off upgrade.

## Impact

- **Catalog context**:
  - `QuantityPeriod` enum and `Amount.Period`.
  - Create and update handlers, DTO and endpoints carry `period`.
  - Migration `AddQuantityPeriod`: a column, plus the data step for the three seeded clothing items.
  - `CatalogSeedData` gets periods.
- **Trips context**:
  - `QuantityPeriod` and `Amount.Period`.
  - `TripDates.Days`/`Nights`.
  - `Trip.ChangePlan(composition, dates)` replaces `ChangeComposition`, and the total includes the period.
  - Migration `AddQuantityPeriod`: a column. Existing totals stay correct, because every existing rule is per uitje.
- **Web**:
  - The trip plan endpoint replaces the composition endpoint.
  - `CategorySelections` passes the period through.
- **Frontend**:
  - Period choice in `QuantityFields`.
  - `describeRule` gets the period suffix, and the rule chip gets a no-dates hint.
  - The trip header dialog becomes "Wie en wanneer?", with date fields.
  - The overview shows the period.
- **Tests**:
  - Domain, handler and repository tests in Catalog and Trips.
  - Migration tests.
  - Web tests for the plan endpoint and the period on items.
- **Dependencies**: none.
