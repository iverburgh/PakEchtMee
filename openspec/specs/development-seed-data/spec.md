# development-seed-data Specification

## Purpose

Guarantees that a local development database contains the family's real packing catalog and two representative trips, so the app is usable right after a fresh setup without overwriting anything the user has created or changed.

## Requirements

### Requirement: Seeding runs only in the development environment

The system SHALL apply the seed data at API startup in the Development environment, after the database migrations have been applied. In any other environment the system SHALL NOT insert seed data.

#### Scenario: Seed applied on development startup

- **WHEN** the API starts in the Development environment against an empty database
- **THEN** after startup the catalog contains the seeded categories and items and the trip list contains the two seeded trips

#### Scenario: No seeding outside development

- **WHEN** the API starts in an environment other than Development
- **THEN** no seed categories, items or trips are inserted

### Requirement: Seeded catalog mirrors the family packing list

The seeded catalog SHALL contain 22 categories with 149 catalog items in total, taken from the sheet "Paklijst vakanties en weekendjes weg 2026". Category names that appear in more than one group of the sheet SHALL be made unique by prefixing the children's categories with `Kinderen – ` and naming the camping group's general column `Kamperen – Algemeen`. Items under "Voor kinderen" without a sub-heading SHALL belong to `Kinderen – Algemeen`.

#### Scenario: Children's and adults' categories coexist

- **WHEN** the seeded catalog is browsed
- **THEN** both `Kinderen – Kleding` and `Kleding` exist as separate categories, each with its own items

#### Scenario: Items without a sub-heading

- **WHEN** the category `Kinderen – Algemeen` is browsed
- **THEN** it lists exactly `Snoetenpoetsers`, `Extra beddengoed` and `Dekens`

#### Scenario: Seeded data obeys catalog rules

- **WHEN** the seeded catalog is validated against the catalog rules
- **THEN** every category name is unique among categories, every item name is unique within its category, and every name is within its maximum length

### Requirement: Multiplied items carry a quantity

A sheet entry written as `<name> <n>x` SHALL be seeded as an item named `<name>` with quantity `n` and no unit.

#### Scenario: Sleeping bags for three children

- **WHEN** the category `Kinderen – Slapen` is browsed
- **THEN** it contains `Slaapzak` with quantity 3, `Matje` with quantity 3 and `Kussen` with quantity 3

### Requirement: Seeded camping trip

The system SHALL seed a trip named `Kamperen Frankrijk 2026` without dates, with all 22 seeded categories selected. All of its trip items SHALL be in status `Active`.

#### Scenario: Camping trip contains the full list

- **WHEN** the trip `Kamperen Frankrijk 2026` is opened
- **THEN** it has 22 categories and 149 trip items, all `Active`

### Requirement: Seeded winter sports trip

The system SHALL seed a trip named `Wintersport Frankrijk 2025` without dates, with every seeded category selected except the six camping categories (`Tent`, `Slapen in tent`, `Kampeerkeuken`, `Afwas`, `Kamperen – Algemeen`, `Was`). Summer-only items SHALL be `Deleted` on this trip; all other items SHALL be `Active`.

#### Scenario: No camping gear on the winter sports trip

- **WHEN** the trip `Wintersport Frankrijk 2025` is opened
- **THEN** it has 16 categories and none of them is a camping category

#### Scenario: Summer items are hidden but restorable

- **WHEN** the trip `Wintersport Frankrijk 2025` is filtered on status `Deleted`
- **THEN** it shows the 16 summer-only items, among them `Zwembandjes` and `Slippers/crocs/teva's`, and each can be restored to `Active`

#### Scenario: Winter sports trip progress

- **WHEN** the trip `Wintersport Frankrijk 2025` is listed
- **THEN** its progress counts 88 items, all `Active`

### Requirement: Seeding never overwrites user data

Seeding SHALL be idempotent: each seeded category, item and trip SHALL be inserted only when it does not exist yet. A seeded record the user has renamed, moved, removed or progressed SHALL be left as it is, and data the user created SHALL be untouched.

#### Scenario: Restart does not duplicate

- **WHEN** the API is started twice in Development
- **THEN** each seeded category, item and trip exists exactly once

#### Scenario: Existing data is kept

- **WHEN** the API starts in Development against a database that already contains user-created categories and trips
- **THEN** those categories and trips are unchanged and the seeded data is added next to them

#### Scenario: Removed seed category stays removed

- **WHEN** the user removes the seeded category `Koken` and the API is restarted in Development
- **THEN** `Koken` is still removed and is not created again

#### Scenario: Packing progress survives a restart

- **WHEN** the user advances a trip item of `Kamperen Frankrijk 2026` to `Packed` and the API is restarted in Development
- **THEN** that trip item is still `Packed`

#### Scenario: Name clash with user data

- **WHEN** the database already holds a user-created, non-removed category named `Keukengerei` and the API starts in Development
- **THEN** startup succeeds, the user's category is unchanged, and the seeded `Keukengerei` category and its items are skipped
