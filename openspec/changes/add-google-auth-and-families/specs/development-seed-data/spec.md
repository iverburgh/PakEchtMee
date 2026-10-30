# Spec Delta

## MODIFIED Requirements

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
