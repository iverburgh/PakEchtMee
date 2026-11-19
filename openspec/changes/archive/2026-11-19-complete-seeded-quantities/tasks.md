# Tasks

## 1. Seed rules and upgrade

- [x] 1.1 Add the `SeedRules` table to `CatalogSeedData`, with the 120 remaining items. `SeedRule` and `SeedItem` gain a quantity and a unit, and `CatalogSeeder` passes the unit through.
  
  Verify with seed-data tests:
  - every seeded item has a quantity;
  - `PerHead` and `SeedRules` do not overlap;
  - every key names an existing seed item;
  - spot checks for `Haringen` 30, `T-shirts` 1 per adult per day, `WC-papier` 1 rol per day, `Shampoo` 1 fles, `Pannen` 3 and `Truien` 2 per child.
- [x] 1.2 Add the catalog migration `CompleteSeededQuantities`. It updates the 120 items from literal rows in one statement, guarded by a general category, no quantity and no unit.
  
  Verify with a Testcontainers migration test:
  - the literal rows equal `SeedRules`;
  - an untouched `Haringen` gets 30;
  - a `Pannen` with quantity 2 is kept.
- [x] 1.3 Update the existing tests and scenarios that expected no quantity (`Pannen`, `Truien`, the count of items with a quantity). Run the whole solution's tests.
- [x] 1.4 Restart Aspire, so the migration runs once on the local database. Check in the browser that the general catalog shows an amount for every item.

## Workflow follow-up

- After the push to `main`, the migration runs once in production. Check the general catalog there.
- Archive the change with `/opsx:archive`.
