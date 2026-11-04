# Tasks

## 1. Family composition (Families context)

- [x] 1.1 Add `FamilyComposition` and `Family.ChangeComposition` (design D2).
  - `FamilyComposition`: adults and children each 0–20, total at least 1, Dutch validation messages.
  - `Family.Create` defaults to 2 adults and 0 children.
  
  Verify with Families.Domain unit tests: default, valid change, both zero, out of range (-1, 21).
- [x] 1.2 Add `GetFamilyCompositionHandler` and `ChangeFamilyCompositionHandler` with `FamilyCompositionDto` (design D5).
  
  Verify with Moq handler tests: success, family not found, validation failure, repository throws.
- [x] 1.3 Map the composition in `FamiliesDbContext` and add the migration `AddFamilyComposition` (defaults 2/0, Verburgh set to 2/3) (design D7).
  
  Verify with Testcontainers repository tests: round-trip, and an existing family reads 2/0.
- [x] 1.4 Add the endpoints `GET/PUT /api/family/composition` for family members (design D6).
  
  Verify with Web tests:
  - a member reads and changes the composition;
  - an administrator without a family gets 403;
  - invalid input gets 400.

## 2. Quantity rule in the catalog (Catalog context)

- [x] 2.1 Add `QuantityBasis` and extend `Amount.Create`/`ToString` (design D1).
  
  Verify with Catalog.Domain unit tests covering the `packing-catalog` scenarios:
  - basis defaults to fixed;
  - "1 per kind";
  - "2 liter per persoon";
  - a basis without a quantity is rejected;
  - the existing quantity and unit cases still pass.
- [x] 2.2 Carry `basis` through the create and update item handlers, `CatalogItemDto`, and `POST/PUT /api/items` (optional; missing means fixed).
  
  Verify with:
  - Moq handler tests (success with a basis, missing basis is fixed, repository failure);
  - a Web test that creates "Jassen" with 1 per child and reads it back.
- [x] 2.3 Persist the basis and add the migration `AddQuantityBasis` (design D7).
  - Column default `'Fixed'`.
  - A data step for the seeded ids from the `development-seed-data` lists, with literal GUIDs, guarded by `Unit IS NULL AND (Quantity IS NULL OR Quantity = 3) AND FamilyId IS NULL`.
  
  Verify with Testcontainers tests:
  - basis round-trip;
  - the guarded update converts an untouched seeded item;
  - the guarded update leaves an item with quantity 4 as it is.
- [x] 2.4 Update `CatalogSeedData` and `CatalogSeeder` with an optional basis per item: the "3x" items become 1 per child, and the per-child, per-adult and per-person lists from the spec are applied.
  
  Verify with seeding tests:
  - `Slaapzak`, `Jassen` and `Kinderen – Toiletartikelen/Handdoek(en) douchen` are per child;
  - `Jas` is per adult;
  - `Borden` is per person;
  - `Pannen` has no quantity;
  - the migration's GUID list equals the ids computed from `CatalogSeedData`.

## 3. Trip composition and totals (Trips context)

- [x] 3.1 Add `QuantityBasis` and the extended `Amount` to Trips.Domain, `TripComposition` with `Multiplier`, and the persisted `TripItem.Total`. `Trip.Create` takes a composition, and `SelectCategories` copies the rule and calculates the total (design D1–D3).
  
  Verify with Trips.Domain unit tests:
  - 1 per child × 3 = 3;
  - 1 per person with 2 + 3 = 5;
  - fixed 20 stays 20;
  - 0 children gives 0;
  - no quantity gives no total.
- [x] 3.2 Add `Trip.ChangeComposition` and `Trip.ChangeItemAmount` (design D4).
  
  Verify with Trips.Domain unit tests covering the `trip-packing` scenarios:
  - an `Active` per-child item is recalculated, while a `Packed` one and a fixed one keep their total;
  - an invalid composition is rejected;
  - a rule change on an `Active` item sets the new total;
  - a change on a `Packed` or deleted item is rejected;
  - clearing the quantity removes the total;
  - a later composition change uses the new rule.
- [x] 3.3 Add the handlers and contracts:
  - `CreateTripHandler.Command` with adults and children;
  - `ChangeTripCompositionHandler` (returns `TripDetailsDto`);
  - `ChangeTripItemAmountHandler` (returns `TripItemDto`);
  - `TripItemDto` and the trip DTOs gain `basis`, `total`, `adults` and `children`.
  
  Verify with Moq handler tests via `TripsHarness`: success, trip not found, validation failure, repository throws.
- [x] 3.4 Map the composition, basis and total in `TripsDbContext` and add the migration `AddQuantityPerPerson` (defaults 2/0, basis `'Fixed'`, `Total = Quantity` for existing items).
  
  Verify with Testcontainers repository tests: round-trip of composition, rule and total; an existing item reads basis fixed with total equal to its quantity.

## 4. Composition root and API

- [x] 4.1 Wire the cross-context paths (design D5):
  - `POST /api/trips` reads the family composition and passes it to `CreateTripHandler`;
  - `CategorySelections` puts the basis in each `CatalogItemSnapshot`;
  - `DevelopmentSeeder` creates the seeded trips with the family's composition.
  
  Verify with Web tests:
  - a trip created by a 2 + 3 family has 2 + 3;
  - adding "Kinderen – Buitenkleding" gives "Jassen" a total of 3;
  - the seeded camping trip has `Slaapzak` with total 3.
- [x] 4.2 Add `PUT /api/trips/{tripId}/composition` and `PUT /api/trips/{tripId}/items/{itemId}/quantity` (design D6).
  
  Verify with Web tests:
  - recalculation is visible in the response;
  - the quantity on a `Packed` item gets 400;
  - another family's trip gets 404;
  - the family composition is unchanged after a trip change.
  
  Then run `dotnet test` for the whole solution.

## 5. Frontend

- [x] 5.1 Add the shared rule helpers to `lib/types.ts`, extend the API types and actions with `basis`, `total`, `adults` and `children`, and add the `QuantityBadge` component with the colour per basis (design D8).
  
  Verify with `npm run lint`, `npx tsc --noEmit`, and a scratch `node --experimental-strip-types` check of `describeRule` ("1 per kind", "2 liter per persoon", "20").
- [x] 5.2 Add the basis radio group to the catalog item dialog, and show the rule in the catalog item list.
  
  Verify with `npm run lint` and `npm run build`.
- [x] 5.3 Trip page changes:
  - show `QuantityBadge` in the walkthrough and the overview;
  - add `QuantityDialog` on `Active` items only;
  - add the composition summary with `CompositionDialog` in the trip header;
  - show toasts after saving.
  
  Verify with `npm run lint` and `npm run build`.
- [x] 5.4 Add the `/gezin` page with the composition form, and link it in the bottom navigation for members.
  
  Verify with `npm run lint` and `npm run build`.
- [x] 5.5 Check in the browser against the Aspire dev environment, at phone and desktop width:
  - the "Gezin" page shows 2 adults and 3 children and saves a change;
  - a new trip takes over the composition;
  - "Jassen" shows 3 with an amber "1 per kind" chip;
  - "Borden" shows 5 with "1 per persoon";
  - changing the trip to 2 children recalculates only the `Te doen` items;
  - the quantity dialog changes "Jassen" to 2 per child (total 4) and is not offered on a packed item;
  - the overview shows totals and rules;
  - the catalog dialog saves a basis.

## Workflow follow-up

- After the push to `main`, check on production that the migrations ran: Verburgh has 2 + 3 and `Slaapzak` is 1 per child.
- Archive the change with `/opsx:archive`.
