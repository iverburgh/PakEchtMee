# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Change a trip's composition`
- TO: `### Requirement: Change who and when for a trip`

## MODIFIED Requirements

### Requirement: Select categories to generate the trip list

The system SHALL let a member select one or more categories available to the family for a trip: its non-removed own categories and the non-removed general categories it has switched on. On selection, the system SHALL create one trip item for every non-removed catalog item in each selected category, copying the item name, category name and quantity rule at that moment, and calculating the item's total from the trip's composition and duration. Every generated trip item SHALL start as `Active`. Adding a category later SHALL add only trip items that do not yet exist for that trip.

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

#### Scenario: Total per child per day

- **WHEN** "Sokken" with the rule 1 per child per day is added to a trip from 2026-07-04 to 2026-07-18 with 3 children
- **THEN** the trip item "Sokken" has a total of 45

### Requirement: Change who and when for a trip

A member SHALL be able to change the number of adults and children and the start and end date of a trip of their family in one step, within the limits of a family composition and the date rules for creating a trip. The change SHALL recalculate the total of every `Active` trip item whose rule depends on the composition or the duration. Items in any other status, and items with a fixed quantity per trip, SHALL keep their total. The family's composition SHALL stay unchanged.

#### Scenario: One child stays home

- **WHEN** a member changes the trip "Summer France" from 3 to 2 children while "Jassen" (1 per child) is `Active` and "Schoenen" (1 per child) is `Packed`
- **THEN** "Jassen" shows a total of 2 and "Schoenen" keeps its total of 3

#### Scenario: Family composition is not changed by a trip

- **WHEN** a member changes the composition of a trip
- **THEN** the family composition and the composition of other trips are unchanged

#### Scenario: Invalid trip composition is rejected

- **WHEN** a member sets a trip to 0 adults and 0 children
- **THEN** the request is rejected with a validation error and no total changes

#### Scenario: A longer trip

- **WHEN** a member moves the end date of a 15-day trip with 3 children one week later, while "Sokken" (1 per child per day) is `Active`
- **THEN** "Sokken" shows a total of 66

#### Scenario: Dates added to a trip without dates

- **WHEN** a member gives the trip "Day at the beach" the dates 2026-08-01 to 2026-08-03
- **THEN** the trip has 3 days and 2 nights, and the per-day and per-night totals of its `Active` items follow from that

#### Scenario: End date before start date is rejected

- **WHEN** a member sets an end date before the start date
- **THEN** the request is rejected with a validation error and no total changes

### Requirement: Change a trip item's quantity while it is to do

A member SHALL be able to change the quantity rule of a single trip item (quantity, basis, period and unit) or remove it, under the same rules as a catalog item. The total SHALL then be calculated from the trip's composition and duration, and later changes of who and when SHALL use the new rule. The change SHALL only be allowed while the item is `Active`. The catalog item SHALL stay unchanged.

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

#### Scenario: Switching to per day

- **WHEN** a member changes the `Active` trip item "Onderbroeken" to 1 per adult per day on a trip of 15 days with 2 adults
- **THEN** "Onderbroeken" shows a total of 30

### Requirement: Trip items show their total and rule

Wherever a trip item with a quantity is shown, the system SHALL show its total prominently, with the unit when present. For a rule per person, per adult or per child, it SHALL also show the rule (for example "1 per kind"), marked with a colour that differs per basis. A rule per day or per night SHALL show its period in the rule (for example "1 per kind per dag" or "2 per nacht"), and on a trip without dates it SHALL also show that the dates are missing. Items with a fixed quantity per trip SHALL show only the total.

#### Scenario: Jackets per child in the walkthrough

- **WHEN** a member opens a trip with 3 children where "Jassen" has the rule 1 per child
- **THEN** the item "Jassen" shows the total 3 and the rule "1 per kind" in the colour for children

#### Scenario: Zero total for a missing group

- **WHEN** a trip has 0 children and contains "Jassen" with the rule 1 per child
- **THEN** "Jassen" shows the total 0 with the rule "1 per kind", so the member can delete or change it

#### Scenario: Fixed quantity without rule

- **WHEN** a trip item "Haringen" has the fixed quantity 20
- **THEN** it shows "20" without a rule

#### Scenario: Rule with a period

- **WHEN** a trip of 15 days with 3 children contains "Sokken" with the rule 1 per child per day
- **THEN** "Sokken" shows the total 45 and the rule "1 per kind per dag"

#### Scenario: Trip without dates

- **WHEN** a trip without dates and with 3 children contains "Sokken" with the rule 1 per child per day
- **THEN** "Sokken" shows the total 3, the rule "1 per kind per dag" and a hint that the trip has no dates yet

## ADDED Requirements

### Requirement: Trip duration counts days and nights

A trip with both a start and an end date SHALL count every calendar day from start to end inclusive as a day, and one night fewer than its days. A per-day or per-night rule SHALL multiply by those days or nights. A trip without both dates SHALL count a per-day or per-night rule once.

#### Scenario: Two weeks in France

- **WHEN** a trip runs from 2026-07-04 to 2026-07-18
- **THEN** it has 15 days and 14 nights

#### Scenario: Day trip

- **WHEN** a trip starts and ends on 2026-08-01
- **THEN** it has 1 day and 0 nights, and a per-night item totals 0

#### Scenario: No dates yet

- **WHEN** a trip without dates and with 2 adults contains "Onderbroeken" with the rule 1 per adult per day
- **THEN** "Onderbroeken" has a total of 2
