# Proposal

## Why

Only 29 of the 149 items in the general catalog have a quantity rule. The other 120, such as `Pannen`, `Haringen`, `Shampoo` and `T-shirts`, show no amount at all on a trip. That leaves the per-person and per-day rules half-used. The family wants every seeded item to say how many to pack. This has to happen once in every existing database (locally and in production) and right away in a fresh one.

## What Changes

- **Seed data**: every seeded general item gets a quantity rule (quantity, optional unit, basis, period).
  - The 29 items that already have one keep it.
  - The other 120 get a rule chosen to fit the family, for example:
    - `Haringen` 30;
    - `Shampoo` 1 fles;
    - `T-shirts` 1 per adult per day;
    - `WC-papier` 1 rol per day;
    - `Rugzakken` 1 per person.
- **One-off upgrade**: a migration gives those 120 items their rule in every existing database, but only while an item still has no quantity and no unit. Anything an administrator has already filled in is kept. It runs once per database, through the usual migration at startup: locally on the next `aspire run`, and in production on the next deploy.
- **Trips**: existing trip items are unchanged, because they are a snapshot. New trips and categories added later get the totals.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `development-seed-data`:
  - The rule that "every other seeded item stays without a quantity" is replaced by "every seeded item carries a quantity rule".
  - The scenarios that relied on `Pannen` and `Truien` having no quantity are updated.
  - A requirement is added for the one-off upgrade of the remaining items.

## Impact

- `src/Catalog/Catalog.Infrastructure.Sql/Seeding/CatalogSeedData.cs`: a second rule table, `SeedRules`, next to `PerHead`. `SeedRule` and `SeedItem` gain a quantity and a unit.
- `CatalogSeeder`: passes the unit through.
- A new catalog migration, `CompleteSeededQuantities`, with the 120 rules as literal rows. It updates the matching items in a single statement.
- Tests:
  - seed-data tests (every item has a rule, spot checks, no overlap between the tables);
  - a migration test (literal rows equal the seed table; untouched items get their rule, changed ones are kept);
  - updated scenarios for `Pannen` and `Truien`.
- No API, frontend or Trips change.
