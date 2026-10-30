# Tasks

## 1. Domain factories with an explicit id

- [x] 1.1 Add `Category.Create(Guid id, CategoryName name)` and let the existing overload delegate with `Guid.CreateVersion7()`; reject `Guid.Empty` with an `ArgumentException`. Verify with new tests in `Catalog.Domain.Tests/Categories/CategoryTests.cs` (keeps the given id, rejects an empty id).
- [x] 1.2 Add `CatalogItem.Create(Guid id, Category category, ItemName name, Amount? amount)` the same way, keeping the "category must not be removed" check. Verify with tests in `Catalog.Domain.Tests/Items/CatalogItemTests.cs` (keeps the id, rejects an empty id, rejects a removed category).
- [x] 1.3 Add `Trip.Create(Guid id, TripName name, TripDates dates)` the same way. Verify with tests in `Trips.Domain.Tests/TripTests.cs` (keeps the id, rejects an empty id).

## 2. Catalog seed definition and seeder

- [x] 2.1 Add a deterministic id helper in `Catalog.Infrastructure.Sql/Seeding/` (name-based UUID over a fixed namespace, design D2). Verify with unit tests: the same key always gives the same id, different keys give different ids, and the result is never `Guid.Empty`.
- [x] 2.2 Add `CatalogSeedData` with the 22 categories and 149 items from the design appendix, including quantity 3 for `Slaapzak`, `Matje` and `Kussen`, and public id lookups for categories and items. Verify with a unit test that asserts 22 categories, 149 items, unique category names, unique item names per category (both case- and accent-insensitive), names within `CategoryName.MaxLength` / `ItemName.MaxLength`, and the three quantities.
- [x] 2.3 Implement `CatalogSeeder` with the idempotence rules of design D4 (lookups include removed records, name clashes are skipped and logged, one `SaveChangesAsync`). Verify with `[IntegrationTest]` tests in `Catalog.Infrastructure.Sql.Tests/CatalogSeederTests.cs`: an empty database gets 22 categories and 149 items; a second run inserts nothing; a removed seed category stays removed and gets no new items; a renamed seed category keeps its new name; a user-created non-removed `Keukengerei` makes the seeder skip that category and its items without failing.

## 3. Trips seeder

- [x] 3.1 Implement `TripsSeeder` in `Trips.Infrastructure.Sql/Seeding/`: given a trip id, name, category selections and source item ids to delete, it creates the trip only when the id does not exist yet, selects the categories, deletes the matching trip items through the aggregate, and saves once (design D4, D6). Verify with `[IntegrationTest]` tests in `Trips.Infrastructure.Sql.Tests/TripsSeederTests.cs`: a new trip gets all selected items with the listed ones `Deleted`; a second run changes nothing; a trip item advanced to `Packed` is still `Packed` after a second run.

## 4. Composition and startup

- [x] 4.1 Move the DTO-to-`CategorySelection` mapping out of `TripEndpoints.SelectCategoriesAsync` into a shared helper in `Web` and use it from the endpoint (design D5). Verify that the existing `TripEndpointsTests` still pass unchanged.
- [x] 4.2 Add `Web/Seeding/DevelopmentSeeder` with the two trip definitions (fixed GUIDs, no dates): `Kamperen Frankrijk 2026` with all seeded categories, `Wintersport Frankrijk 2025` with the 16 non-camping categories and the 16 summer items from the design appendix deleted. It runs the catalog seeder, reads the live catalog through the existing queries, and runs the trips seeder; it logs the inserted counts. Verify with a unit-level check that the summer-item list contains exactly 16 ids that all exist in `CatalogSeedData`.
- [x] 4.3 Call `DevelopmentSeeder` from `Program.cs` inside the Development block after both `MigrateAsync` calls. Verify with Web integration tests: a fixture hosting the API in `Development` against an empty database returns 22 categories and both trips (`Kamperen Frankrijk 2026` with 149 items, `Wintersport Frankrijk 2025` with 88 non-deleted and 16 deleted items); the existing `Testing` fixture still returns no categories on a fresh database.

## 5. End-to-end check

- [x] 5.1 Run `dotnet build` and `dotnet test` for the solution and verify both succeed without warnings.
- [x] 5.2 Start the app with `aspire run`, open the frontend, and verify the catalog shows the 22 categories, the trip list shows both seeded trips next to the existing local data, and restarting the AppHost creates no duplicates.

## Workflow follow-up

- Run `/opsx:archive` after review to merge `development-seed-data` into `openspec/specs/`.
