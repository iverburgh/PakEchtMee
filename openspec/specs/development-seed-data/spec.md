# development-seed-data Specification

## Purpose

Guarantees that a local development database contains the family's real packing catalog and two representative trips, so the app is usable right after a fresh setup without overwriting anything the user has created or changed.

## Requirements

### Requirement: Catalog is seeded in every environment, trips only in development

The system SHALL apply the seed data at API startup, after the database migrations have been applied. The general catalog from the family packing list SHALL be seeded in every environment. The two seeded trips SHALL be seeded only in the Development environment.

#### Scenario: Seed applied on development startup

- **WHEN** the API starts in the Development environment against an empty database
- **THEN** after startup the catalog contains the seeded categories and items and the trip list contains the two seeded trips

#### Scenario: Only the catalog outside development

- **WHEN** the API starts in an environment other than Development against an empty database
- **THEN** the general catalog contains the seeded categories and items, and no seed trips are inserted

### Requirement: Seeded catalog mirrors the family packing list

The seeded catalog SHALL be general - belonging to no family - and SHALL contain 22 categories with 149 catalog items in total, taken from the sheet "Paklijst vakanties en weekendjes weg 2026". Category names that appear in more than one group of the sheet SHALL be made unique by prefixing the children's categories with `Kinderen – ` and naming the camping group's general column `Kamperen – Algemeen`. Items under "Voor kinderen" without a sub-heading SHALL belong to `Kinderen – Algemeen`.

#### Scenario: Children's and adults' categories coexist

- **WHEN** the seeded catalog is browsed
- **THEN** both `Kinderen – Kleding` and `Kleding` exist as separate categories, each with its own items

#### Scenario: Items without a sub-heading

- **WHEN** the category `Kinderen – Algemeen` is browsed
- **THEN** it lists exactly `Snoetenpoetsers`, `Extra beddengoed` and `Dekens`

#### Scenario: Seeded data obeys catalog rules

- **WHEN** the seeded catalog is validated against the catalog rules
- **THEN** every category name is unique among categories, every item name is unique within its category, and every name is within its maximum length

#### Scenario: Seeded categories are general

- **WHEN** a member of any family opens the catalog after seeding
- **THEN** the 22 seeded categories are listed as general categories and cannot be edited by that member

### Requirement: Multiplied items carry a quantity

A sheet entry written as `<name> <n>x` in a `Kinderen – …` category SHALL be seeded as an item named `<name>` with quantity 1 per child and no unit, because the multiplier on the sheet is the number of children.

#### Scenario: Sleeping bags for three children

- **WHEN** the category `Kinderen – Slapen` is browsed
- **THEN** it contains `Slaapzak`, `Matje` and `Kussen`, each with the rule 1 per child

### Requirement: Seeded children's items are per child

The seeded catalog SHALL give these items the rule 1 per child, without a unit:

- `Kinderen – Toiletartikelen`: `Handdoek(en) douchen`
- `Kinderen – Buitenkleding`: `Jassen`, `Laarzen`, `Schoenen`, `Crocs/sandalen/slippers`, `Petten`, `Zonnebrillen`
- `Kinderen – Vermaak op locatie`: `Zwemkleding`, `Handdoek(en) zwemmen`

#### Scenario: Jackets per child

- **WHEN** the category `Kinderen – Buitenkleding` is browsed after seeding
- **THEN** `Jassen` has the rule 1 per child

### Requirement: Seeded adult items are per adult

The seeded catalog SHALL give these items the rule 1 per adult, without a unit:

- `Buitenkleding`: `Jas`, `Slippers/crocs/teva's`, `Pet`, `Zonnebrillen`, `Wandelschoenen`
- `Toiletartikelen`: `Handdoeken douchen`
- `Slapen in tent`: `Matjes`, `Kussens`

#### Scenario: Adult jacket per adult

- **WHEN** the category `Buitenkleding` is browsed after seeding
- **THEN** `Jas` has the rule 1 per adult

### Requirement: Seeded shared items are per person

The seeded catalog SHALL give these items the rule 1 per person, without a unit. Every seeded item not named in this or the per-child or per-adult lists SHALL stay without a quantity.

- `Vermaak op locatie`: `Badhanddoeken`
- `Algemeen`: `Geldige paspoorten`
- `Kampeerkeuken`: `Borden`, `Bekers`, `Bestek`
- `Kamperen – Algemeen`: `Stoelen`

#### Scenario: Plates per person

- **WHEN** the category `Kampeerkeuken` is browsed after seeding
- **THEN** `Borden` has the rule 1 per person and `Pannen` has no quantity

### Requirement: Untouched seeded items get their rule on upgrade

On upgrade, each seeded general item from the per-child, per-adult and per-person lists SHALL get its rule only while it still has its originally seeded amount: no unit, and either no quantity or the seeded quantity 3. An item whose amount was changed SHALL be left as it is. Existing trip items SHALL be unchanged.

#### Scenario: Untouched item is converted

- **WHEN** the database contains the seeded `Slaapzak` with quantity 3 and no unit, and the app is upgraded
- **THEN** `Slaapzak` has the rule 1 per child

#### Scenario: Changed item is kept

- **WHEN** the administrator had given the seeded `Jassen` the quantity 4 before the upgrade
- **THEN** `Jassen` keeps the fixed quantity 4 after the upgrade

#### Scenario: Existing trips are unchanged

- **WHEN** the trip `Kamperen Frankrijk 2026` exists before the upgrade
- **THEN** its trip items keep their quantities, with a fixed basis

### Requirement: Seeded trips use the family composition

Seeded trips SHALL be created with the composition of their family at seeding time.

#### Scenario: Seeded camping trip on a fresh database

- **WHEN** a fresh development database is seeded and "Verburgh" has 2 adults and 3 children
- **THEN** `Kamperen Frankrijk 2026` has 2 adults and 3 children, and its `Slaapzak` item has a total of 3

### Requirement: Seeded camping trip

The system SHALL seed a trip named `Kamperen Frankrijk 2026` for the family "Verburgh", without dates, with all 22 seeded categories selected. All of its trip items SHALL be in status `Active`.

#### Scenario: Camping trip contains the full list

- **WHEN** a member of "Verburgh" opens the trip `Kamperen Frankrijk 2026`
- **THEN** it has 22 categories and 149 trip items, all `Active`

#### Scenario: Camping trip is private to Verburgh

- **WHEN** a member of another family opens the trip list
- **THEN** `Kamperen Frankrijk 2026` is not listed

### Requirement: Seeded winter sports trip

The system SHALL seed a trip named `Wintersport Frankrijk 2025` for the family "Verburgh", without dates, with every seeded category selected except the six camping categories (`Tent`, `Slapen in tent`, `Kampeerkeuken`, `Afwas`, `Kamperen – Algemeen`, `Was`). Summer-only items SHALL be `Deleted` on this trip; all other items SHALL be `Active`.

#### Scenario: No camping gear on the winter sports trip

- **WHEN** a member of "Verburgh" opens the trip `Wintersport Frankrijk 2025`
- **THEN** it has 16 categories and none of them is a camping category

#### Scenario: Summer items are hidden but restorable

- **WHEN** the trip `Wintersport Frankrijk 2025` is filtered on status `Deleted`
- **THEN** it shows the 16 summer-only items, among them `Zwembandjes` and `Slippers/crocs/teva's`, and each can be restored to `Active`

#### Scenario: Winter sports trip progress

- **WHEN** the trip `Wintersport Frankrijk 2025` is listed
- **THEN** its progress counts 88 items, all `Active`

### Requirement: Seeding never overwrites user data

Seeding SHALL be idempotent: each seeded category, item and trip SHALL be inserted only when it does not exist yet. A seeded record that has been renamed, moved, removed or progressed SHALL be left as it is, and data created by users SHALL be untouched. Name clashes SHALL only be checked against the general catalog.

#### Scenario: Restart does not duplicate

- **WHEN** the API is started twice in Development
- **THEN** each seeded category, item and trip exists exactly once

#### Scenario: Existing data is kept

- **WHEN** the API starts in Development against a database that already contains family categories and trips
- **THEN** those categories and trips are unchanged and the seeded data is added next to them

#### Scenario: Removed seed category stays removed

- **WHEN** the administrator removes the seeded general category `Koken` and the API is restarted in Development
- **THEN** `Koken` is still removed and is not created again

#### Scenario: Packing progress survives a restart

- **WHEN** a member advances a trip item of `Kamperen Frankrijk 2026` to `Packed` and the API is restarted in Development
- **THEN** that trip item is still `Packed`

#### Scenario: Name clash with user data

- **WHEN** the general catalog already holds a non-removed category named `Keukengerei` that the administrator created, and the API starts in Development
- **THEN** startup succeeds, that category is unchanged, and the seeded `Keukengerei` category and its items are skipped

#### Scenario: Family category with a seed name is no clash

- **WHEN** "Verburgh" has an own category named `Keukengerei` and the API starts in Development against a database without the seed
- **THEN** the seeded general `Keukengerei` category is created next to the family's own one
