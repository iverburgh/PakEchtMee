# Spec Delta

## MODIFIED Requirements

### Requirement: Create a trip

The system SHALL allow a family member to create a trip for their family with a non-empty name of at most 100 characters and an optional start date and end date. When both dates are given, the end date SHALL NOT be earlier than the start date. A new trip SHALL take over the family's number of adults and children as its own composition. Trips SHALL be listed with the most relevant (upcoming or ongoing) trip first.

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

#### Scenario: New trip takes over the family composition

- **WHEN** a member of "Verburgh", which has 2 adults and 3 children, creates the trip "Summer France"
- **THEN** "Summer France" has 2 adults and 3 children

### Requirement: Select categories to generate the trip list

The system SHALL let a member select one or more categories available to the family for a trip: its non-removed own categories and the non-removed general categories it has switched on. On selection, the system SHALL create one trip item for every non-removed catalog item in each selected category, copying the item name, category name and quantity rule at that moment, and calculating the item's total from the trip's composition. Every generated trip item SHALL start as `Active`. Adding a category later SHALL add only trip items that do not yet exist for that trip.

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

- **WHEN** a catalog item is renamed, moved, removed or given another quantity rule after a trip list has been generated
- **THEN** the already generated trip items of that trip keep their copied name, category and quantity rule

#### Scenario: Total calculated from the trip composition

- **WHEN** a member adds "Kinderen – Buitenkleding" to a trip with 2 adults and 3 children, and "Jassen" has the rule 1 per child
- **THEN** the trip item "Jassen" has a total of 3

#### Scenario: Per-person total counts adults and children

- **WHEN** a catalog item "Borden" with the rule 1 per person is added to a trip with 2 adults and 3 children
- **THEN** the trip item "Borden" has a total of 5

#### Scenario: Fixed quantity is copied as is

- **WHEN** a catalog item "Haringen" with the fixed quantity 20 is added to a trip
- **THEN** the trip item "Haringen" has a total of 20, whatever the trip's composition

## ADDED Requirements

### Requirement: Change a trip's composition

A member SHALL be able to change the number of adults and children of a trip of their family, within the same limits as a family composition. The change SHALL recalculate the total of every `Active` trip item whose rule is per person, per adult or per child. Trip items in any other status, and items with a fixed quantity, SHALL keep their total. The family's composition SHALL stay unchanged.

#### Scenario: One child stays home

- **WHEN** a member changes the trip "Summer France" from 3 to 2 children while "Jassen" (1 per child) is `Active` and "Schoenen" (1 per child) is `Packed`
- **THEN** "Jassen" shows a total of 2 and "Schoenen" keeps its total of 3

#### Scenario: Family composition is not changed by a trip

- **WHEN** a member changes the composition of a trip
- **THEN** the family composition and the composition of other trips are unchanged

#### Scenario: Invalid trip composition is rejected

- **WHEN** a member sets a trip to 0 adults and 0 children
- **THEN** the request is rejected with a validation error and no total changes

### Requirement: Change a trip item's quantity while it is to do

A member SHALL be able to change the quantity rule of a single trip item (quantity, basis and unit) or remove it, under the same rules as a catalog item. The total SHALL then be calculated from the trip's composition, and later composition changes SHALL use the new rule. The change SHALL only be allowed while the item is `Active`. The catalog item SHALL stay unchanged.

#### Scenario: Two extra jackets

- **WHEN** a member changes the `Active` trip item "Jassen" from 1 per child to 2 per child on a trip with 3 children
- **THEN** "Jassen" shows a total of 6, and the catalog item "Jassen" still has the rule 1 per child

#### Scenario: Switching to a fixed number

- **WHEN** a member changes the `Active` trip item "Handdoeken" to the fixed quantity 4
- **THEN** "Handdoeken" shows a total of 4 and keeps it after the trip's composition changes

#### Scenario: Progressed item cannot be changed

- **WHEN** a member tries to change the quantity of a `Packed` trip item
- **THEN** the request is rejected with a validation error, and the walkthrough offers no quantity action for that item

#### Scenario: Removing the quantity

- **WHEN** a member removes the quantity of the `Active` trip item "Paraplu"
- **THEN** "Paraplu" is shown without an amount

### Requirement: Trip items show their total and rule

Wherever a trip item with a quantity is shown, the system SHALL show its total prominently, with the unit when present. For a rule per person, per adult or per child, it SHALL also show the rule (for example "1 per kind"), marked with a colour that differs per basis. Items with a fixed quantity SHALL show only the total.

#### Scenario: Jackets per child in the walkthrough

- **WHEN** a member opens a trip with 3 children where "Jassen" has the rule 1 per child
- **THEN** the item "Jassen" shows the total 3 and the rule "1 per kind" in the colour for children

#### Scenario: Zero total for a missing group

- **WHEN** a trip has 0 children and contains "Jassen" with the rule 1 per child
- **THEN** "Jassen" shows the total 0 with the rule "1 per kind", so the member can delete or change it

#### Scenario: Fixed quantity without rule

- **WHEN** a trip item "Haringen" has the fixed quantity 20
- **THEN** it shows "20" without a rule
