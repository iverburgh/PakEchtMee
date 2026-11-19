# Tasks

## 1. Period in the catalog (Catalog context)

- [x] 1.1 Add `QuantityPeriod`, `QuantityPeriods.Parse` and its suffix, and extend `Amount.Create`/`ToString` with a period (design D1).
  
  Verify with Catalog.Domain unit tests:
  - the default is per trip;
  - "1 per kind per dag", "2 per nacht" and "2 liter per persoon per dag";
  - a period without a quantity is rejected;
  - an unknown API value is rejected;
  - existing amount tests still pass.
- [x] 1.2 Carry `period` through the create and update item handlers, `CatalogItemDto`, and `POST/PUT /api/items` (design D5).
  
  Verify with Moq handler tests:
  - success with a period;
  - a missing period is per trip;
  - an unknown period gives a validation failure;
  - a repository failure.
- [x] 1.3 Persist the period and add the `AddQuantityPeriod` migration (design D6):
  - back-fill `'PerTrip'` where a quantity exists;
  - a data step for the three seeded clothing ids, guarded by a general category with no quantity and no unit.
  
  Verify with Testcontainers tests:
  - a period round-trips;
  - an item without an amount round-trips;
  - the migration converts the untouched `Kleding/Sokken`;
  - the migration keeps `Onderbroeken` with quantity 10 as fixed per trip;
  - existing amounts read as per trip.
- [x] 1.4 Turn `CatalogSeedData.PerHead` into `SeedRule` values with a period, and add the three per-day clothing items. `SeedItem` and `CatalogSeeder` carry the period, and the existing `AddQuantityBasis` migration test filters on per trip.
  
  Verify with seed tests:
  - `Kinderen – Kleding/Sokken` is 1 per child per day;
  - `Kleding/Onderbroeken` is 1 per adult per day;
  - `Truien` has no quantity;
  - the new migration's GUID list equals the per-day entries of `PerHead`.

## 2. Duration and plan on a trip (Trips context)

- [x] 2.1 Add `QuantityPeriod` to Trips.Domain and the period to `Amount`. Add `TripDates.Days`/`Nights`/`Multiplier`, and make `Amount.TotalFor(composition, dates)` include the period (design D1, D2). `SelectCategories` and `ChangeItemAmount` pass the dates.
  
  Verify with Trips.Domain unit tests:
  - 4 to 18 July has 15 days and 14 nights;
  - a day trip has 1 day and 0 nights;
  - no dates gives null and a multiplier of 1;
  - 1 per child per day × 3 × 15 = 45;
  - fixed 2 per night × 14 = 28;
  - per day without dates counts once.
- [x] 2.2 Replace `Trip.ChangeComposition` with `Trip.ChangePlan(composition, dates)`, and `FollowComposition` with `FollowPlan` (design D3).
  
  Verify with Trips.Domain unit tests covering the "Change who and when for a trip" scenarios:
  - one child stays home;
  - a longer trip turns 45 into 66;
  - dates are added to a trip without dates;
  - a packed or fixed-per-trip item keeps its total;
  - an invalid composition or dates are rejected without changes;
  - the count of changed totals is right.
- [x] 2.3 Rename the handler and DTO to `ChangeTripPlanHandler` and `TripPlanChangedDto`, add `Days`/`Nights` to `TripDetailsDto`, and carry `period` in `ChangeTripItemAmountHandler` and `TripItemDto` (design D4, D5).
  
  Verify with Moq handler tests:
  - success with recalculation;
  - trip not found;
  - invalid dates;
  - repository failure;
  - a quantity change with a period returns the new total.
- [x] 2.4 Persist the trip item period and add the `AddQuantityPeriod` migration (back-fill `'PerTrip'`, design D6).
  
  Verify with Testcontainers tests:
  - rule, period, total and changed dates round-trip;
  - a pre-existing item reads as per trip with its total unchanged.

## 3. Composition root and API

- [x] 3.1 Replace `PUT /api/trips/{id}/composition` with `PUT /api/trips/{id}/plan`, and pass the period through `CategorySelections` and the quantity endpoint (design D4, D5).
  
  Verify with Web tests:
  - a catalog item with a period round-trips;
  - a 15-day trip with 3 children gives "Sokken" a total of 45;
  - the plan change to one week longer gives 66, and the response carries days and nights;
  - end before start gets 400;
  - another family's trip gets 404;
  - the seeded camping trip without dates gives `Kinderen – Kleding/Sokken` a total of 3.
  
  Then run the whole solution's tests.

## 4. Frontend

- [x] 4.1 Add the `QuantityPeriod` types, labels and suffixes, extend `describeRule`, add the API and action changes (`period`, `days`, `nights`, the plan endpoint and action), and add the "Hoe vaak" group in `QuantityFields` (design D7).
  
  Verify with `npm run lint`, `npx tsc --noEmit`, and a scratch `node --experimental-strip-types` check of `describeRule` ("1 per kind per dag", "2 per nacht", "2 liter per persoon per dag", "20").
- [x] 4.2 Update the display:
  - `RuleChip`/`QuantityBadge` show the period and the "geen datums" hint;
  - the trip header becomes `PlanDialog` with steppers and date inputs;
  - the overview shows the rule with its period and the duration.
  
  Verify with `npm run lint` and `npm run build`.
- [x] 4.3 Check in the browser against the Aspire dev environment, at phone and desktop width:
  - in the catalog, `Kinderen – Kleding/Sokken` shows "1 per kind per dag" and a new item can be saved with "2 per nacht";
  - the seeded camping trip shows the "geen datums" hint at `Sokken` with total 3;
  - in "Wie en wanneer?", setting 4 to 18 July turns `Sokken` (if added after the upgrade) into 45, and a packed item keeps its total;
  - the quantity dialog on an item still to do offers the period;
  - the overview shows the period and the duration.

## Workflow follow-up

- After the push to `main`, check on production that `Kleding/Sokken` is 1 per adult per day.
- Archive the change with `/opsx:archive`.
