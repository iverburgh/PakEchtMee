# Proposal

## Why

The packing catalog the family actually uses lives in an Excel sheet ("Paklijst vakanties en weekendjes weg 2026"). A fresh local database starts empty, so every new developer setup or database reset means typing 22 categories and about 150 items in by hand before the app is useful to try out. Seeding that list, together with two realistic trips, makes a local run immediately representative of real use.

## What Changes

- Add a development-only seeder that runs at API startup after the migrations and inserts:
  - the 22 categories and 149 catalog items from the Excel sheet, and
  - two trips: **Kamperen Frankrijk 2026** and **Wintersport Frankrijk 2025**, each with the categories relevant to that trip.
- The seeder is idempotent: every seeded record has a fixed, deterministic id and is only inserted when no record with that id exists. It works on an empty database and on an existing one with other data, and it never resurrects, renames or overwrites seeded records the user has since changed or removed.
- Category names that occur more than once in the sheet are made unique with a `Kinderen – ` prefix for the children's group (for example `Kinderen – Kleding` next to `Kleding`) and `Kamperen – Algemeen` for the camping group's general column. Items listed directly under "Voor kinderen" without a sub-heading go to `Kinderen – Algemeen`.
- Items written as "<name> 3x" in the sheet are seeded as `<name>` with quantity 3.
- For **Wintersport Frankrijk 2025**, summer-only items (swim gear, summer outdoor games, slippers) are put on `Deleted` within that trip, so they are hidden but can be restored.
- The domain gets factory overloads that accept a caller-chosen id for `Category`, `CatalogItem` and `Trip`, so the seeder goes through the domain instead of around it.

No behaviour changes outside the Development environment; nothing is seeded in other environments.

## Capabilities

### New Capabilities

- `development-seed-data`: The reference data a local development database is guaranteed to contain - which categories, items and trips are seeded, when seeding runs, and how it coexists with data the user has created or changed.

### Modified Capabilities

(none - the catalog and trip rules in `packing-catalog` and `trip-packing` apply to seeded data unchanged)

## Impact

- **Catalog context**: `Category` and `CatalogItem` get a `Create` overload with an explicit id; a `CatalogSeeder` plus the seed definition in `Catalog.Infrastructure.Sql`.
- **Trips context**: `Trip` gets a `Create` overload with an explicit id; a `TripsSeeder` in `Trips.Infrastructure.Sql`.
- **Web**: `Program.cs` calls the seeding after `MigrateAsync` in Development; a small composition class maps catalog seed data to trip category selections, because only the composition root may see both contexts.
- **Database**: no schema change and no migration.
- **Tests**: domain tests for the new overloads; Testcontainers integration tests for both seeders (empty database, re-run, coexisting data, user-deleted seed record).
- **Local data**: the existing local database (persistent volume) receives the seed on the next start next to its current test data.
