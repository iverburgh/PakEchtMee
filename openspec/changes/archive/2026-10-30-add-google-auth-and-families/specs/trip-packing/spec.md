# Spec Delta

## MODIFIED Requirements

### Requirement: Create a trip

The system SHALL allow a family member to create a trip for their family with a non-empty name of at most 100 characters and an optional start date and end date. When both dates are given, the end date SHALL NOT be earlier than the start date. Trips SHALL be listed with the most relevant (upcoming or ongoing) trip first.

#### Scenario: Create a trip with dates

- **WHEN** a member of "Verburgh" creates the trip "Summer France" with start 2026-07-04 and end 2026-07-18
- **THEN** the trip is stored for "Verburgh" and appears in its trip list

#### Scenario: Create a trip without dates

- **WHEN** a member creates the trip "Day at the beach" without dates
- **THEN** the trip is stored and appears in the trip list of the member's family

#### Scenario: End date before start date is rejected

- **WHEN** a user submits a trip whose end date precedes its start date
- **THEN** the system rejects the request with a validation error and no trip is created

#### Scenario: Administrator without a family cannot create a trip

- **WHEN** an administrator who is not a member of any family tries to create a trip
- **THEN** the system rejects the request with status 403

### Requirement: Select categories to generate the trip list

The system SHALL let a member select one or more categories available to the family for a trip: its non-removed own categories and the non-removed general categories it has switched on. On selection, the system SHALL create one trip item for every non-removed catalog item in each selected category, copying the item name, category name, quantity and unit at that moment. Every generated trip item SHALL start as `Active`. Adding a category later SHALL add only trip items that do not yet exist for that trip.

#### Scenario: Generating the list from selected categories

- **WHEN** a user selects "Toiletries" (4 items) and "Electronics" (3 items) for a trip
- **THEN** the trip contains 7 trip items, all with status `Active`

#### Scenario: Mixing own and general categories

- **WHEN** a member of "Verburgh" selects the own category "Toiletries" (4 items) and the general category "Tent" (6 items)
- **THEN** the trip contains 10 trip items, all with status `Active`

#### Scenario: Category not available to the family is ignored

- **WHEN** a request selects a general category the family switched off, or an own category of another family
- **THEN** no trip items are created for that category

#### Scenario: Adding a category after generation

- **WHEN** a user adds the category "Ski gear" to an existing trip
- **THEN** the items of that category are appended as `Active` trip items and the statuses of existing trip items are unchanged

#### Scenario: Re-adding a category does not duplicate items

- **WHEN** a user removes a category from a trip and then adds it again
- **THEN** each catalog item of that category is present exactly once in the trip

#### Scenario: Removing a category from a trip

- **WHEN** a user removes the category "Ski gear" from a trip
- **THEN** the system asks for confirmation when any of its trip items has progressed beyond `Active`, and on confirmation those trip items are set to `Deleted`

#### Scenario: Trip items are a snapshot of the catalog

- **WHEN** a catalog item is renamed, moved or removed after a trip list has been generated
- **THEN** the already generated trip items of that trip keep their copied name, category and amount

## ADDED Requirements

### Requirement: Trips are private to their family

A trip SHALL belong to exactly one family. Only members of that family SHALL be able to see the trip or change it and its items; for anyone else the trip SHALL be answered as if it does not exist.

#### Scenario: Trip list shows only the family's trips

- **WHEN** a member of "Jansen" opens the trip list
- **THEN** only trips of "Jansen" are listed

#### Scenario: Another family's trip is not found

- **WHEN** a member of "Jansen" opens or advances an item of a trip of "Verburgh" by its id
- **THEN** the system responds with not found and the trip is unchanged
