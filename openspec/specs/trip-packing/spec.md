# trip-packing Specification

## Purpose

Covers a trip (a holiday or a day trip): choosing which catalog categories to take, generating the trip's packing list from them, and moving every trip item through its packing lifecycle quickly and reliably.

## Requirements

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

### Requirement: Select all categories for a trip at once

When choosing categories for a trip, the system SHALL offer one "Alles" control at the top of the list. It SHALL show as checked when every listed category is selected (already on the trip or ticked to be added), as partly checked when only some are, and as unchecked when none are. Activating it while it is not fully checked SHALL tick every listed category that is not yet on the trip, without saving anything until the member confirms the additions.

#### Scenario: Selecting every category on a new trip

- **WHEN** a member opens the categories of a trip without categories and activates "Alles"
- **THEN** every listed category is ticked, "Alles" shows as checked, and the add button offers to add all of them

#### Scenario: Nothing is saved before confirming

- **WHEN** a member activates "Alles" and leaves the page without confirming the additions
- **THEN** the trip's categories and trip items are unchanged

#### Scenario: Completing a partial selection

- **WHEN** a trip already has "Tent" and "Afwas", the member has ticked "Keukengerei", and "Alles" shows as partly checked
- **THEN** activating "Alles" ticks every remaining category, and confirming adds only the categories that were not yet on the trip

#### Scenario: No categories available

- **WHEN** the family has no categories available for trips
- **THEN** no "Alles" control is shown

### Requirement: Clearing all ticks keeps categories already on the trip

Activating "Alles" while it is checked SHALL clear only the categories ticked to be added. Categories already on the trip SHALL stay on it; removing them SHALL keep going one category at a time, with its existing confirmation. When every listed category is already on the trip and none is waiting to be added, "Alles" SHALL show as checked and SHALL NOT be activatable.

#### Scenario: Clearing pending ticks

- **WHEN** a trip has "Tent", the member ticked every other category via "Alles", and activates "Alles" again
- **THEN** only "Tent" stays ticked, "Alles" shows as partly checked, and no add button is shown

#### Scenario: Clearing never removes categories from the trip

- **WHEN** a trip has "Tent" with packed items and the member clears all ticks with "Alles"
- **THEN** "Tent" and its trip items, including their statuses, are unchanged and no confirmation is asked

#### Scenario: Every category already on the trip

- **WHEN** every listed category is already on the trip and none is waiting to be added
- **THEN** "Alles" shows as checked and cannot be activated

### Requirement: Trip item status lifecycle

Every trip item SHALL have exactly one status from `Active`, `Prepared`, `Packed`, `Loaded`, `Deleted`. The forward flow SHALL be `Active -> Prepared -> Packed -> Loaded`, where each forward transition moves to exactly one next status. `Loaded` SHALL have no next status. `Deleted` SHALL be reachable from any other status and SHALL NOT be part of the forward flow. The system SHALL reject any transition that is not a single forward step, a single backward step, a transition to `Deleted`, or a restore from `Deleted`.

#### Scenario: Advancing through the flow

- **WHEN** a user advances an `Active` item three times
- **THEN** the item passes through `Prepared` and `Packed` and ends in `Loaded`

#### Scenario: Loaded has no next status

- **WHEN** a user tries to advance an item that is `Loaded`
- **THEN** the system does not change the status and the advance action is unavailable for that item

#### Scenario: Correcting a mistake by moving back

- **WHEN** a user moves a `Packed` item back
- **THEN** the item returns to `Prepared`

#### Scenario: Active cannot move further back

- **WHEN** a user tries to move an `Active` item back
- **THEN** the system does not change the status and the back action is unavailable for that item

#### Scenario: Invalid transition is rejected

- **WHEN** a request asks to move an `Active` item directly to `Loaded`
- **THEN** the system rejects the request with a validation error and the item stays `Active`

### Requirement: Delete and restore a trip item

The system SHALL allow a user to set any trip item to `Deleted` and to restore it. A `Deleted` item SHALL be excluded from the walkthrough, from search results, from filters by progress status, and from progress counts, unless the user explicitly asks to see deleted items. Restoring SHALL return the item to the status it held before deletion.

#### Scenario: Deleting an item

- **WHEN** a user deletes the trip item "Ski poles"
- **THEN** the item no longer appears in the default trip list and no longer counts towards trip progress

#### Scenario: Restoring a deleted item

- **WHEN** a user restores a trip item that was `Packed` before deletion
- **THEN** the item is visible again with status `Packed`

#### Scenario: Viewing deleted items explicitly

- **WHEN** a user filters on status `Deleted`
- **THEN** the deleted trip items of that trip are listed with a restore action

### Requirement: Fast walkthrough of trip items

The system SHALL provide a walkthrough of a trip's items in which a single tap on an item advances it to its next status without any confirmation dialog or page navigation. Items in `Loaded` SHALL NOT offer an advance action. After each advance, the system SHALL show the new status and offer an undo that reverts that single item to its previous status. The walkthrough SHALL keep the user's scroll position and SHALL remain usable when an item's status change fails, by restoring the previous status and reporting the failure.

#### Scenario: One tap advances an item

- **WHEN** a user taps an `Active` item in the walkthrough
- **THEN** the item immediately shows status `Prepared` without leaving the list

#### Scenario: Undo after an advance

- **WHEN** a user taps the undo action shown after advancing an item to `Packed`
- **THEN** that item returns to `Prepared` and no other item changes

#### Scenario: Walkthrough keeps position

- **WHEN** a user advances the twentieth item of a long list
- **THEN** the list stays at the same scroll position and the next item remains reachable

#### Scenario: Failed status change is reverted

- **WHEN** a status change cannot be persisted
- **THEN** the item visibly returns to its previous status and the user is shown an error message

### Requirement: Walkthrough is grouped per category

The walkthrough SHALL show a trip's visible items grouped per category, with the categories in alphabetical order and each group under a heading with the category name. A category without visible items after search and filters SHALL get no heading. Items SHALL NOT repeat their category name.

#### Scenario: Items under their category heading

- **WHEN** a member opens a trip with the categories "Algemeen" and "Afwas"
- **THEN** the list shows the heading "Afwas" with its items, followed by the heading "Algemeen" with its items

#### Scenario: Filtered category has no heading

- **WHEN** the member filters on the category "Afwas"
- **THEN** only the heading "Afwas" and its items are shown

#### Scenario: Deleted items in their own category

- **WHEN** the member filters on status `Deleted`
- **THEN** each deleted item is shown under the heading of its own category

### Requirement: Category heading shows the category's progress

Each category heading SHALL show how many of the category's non-deleted items are `Loaded`, out of its non-deleted items. The count SHALL cover the whole category regardless of search and filters, and SHALL update immediately after every status change.

#### Scenario: Progress per category

- **WHEN** the category "Afwas" has 3 non-deleted items of which 1 is `Loaded`
- **THEN** its heading shows 1/3 loaded

#### Scenario: Progress updates after an advance

- **WHEN** the member advances a `Packed` item of "Afwas" to `Loaded`
- **THEN** the heading of "Afwas" immediately shows one more loaded item

### Requirement: Category heading stays visible while scrolling

While the member scrolls through the items of a category, its heading SHALL stay visible at the top of the screen until the next category's heading takes its place.

#### Scenario: Heading stays on top

- **WHEN** the member scrolls halfway through the items of a long category
- **THEN** the heading of that category is visible at the top of the screen

### Requirement: Categories can be collapsed

Activating a category heading SHALL collapse or expand that category's items. Categories SHALL start expanded when the trip is opened. While a search query is entered, every category with matching items SHALL be shown expanded, and clearing the query SHALL restore the collapsed state the member had chosen.

#### Scenario: Collapsing a packed category

- **WHEN** the member activates the heading of "Afwas"
- **THEN** the items of "Afwas" are hidden, the heading with its progress stays visible, and activating it again shows the items

#### Scenario: Search never hides a match

- **WHEN** "Afwas" is collapsed and the member searches for "spons"
- **THEN** "Sponsje + borstel" is shown under the heading "Afwas", and after clearing the search "Afwas" is collapsed again

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

### Requirement: Trip progress

The system SHALL show, per trip, how many non-deleted items are in each status and the share of items that reached `Loaded`. The counts SHALL update immediately after every status change.

#### Scenario: Progress after advancing items

- **WHEN** a trip has 10 non-deleted items of which 4 are `Loaded`
- **THEN** the trip shows 4 of 10 loaded together with the counts of the other statuses

#### Scenario: Deleted items excluded from progress

- **WHEN** a user deletes an `Active` item from a trip of 10 items
- **THEN** the progress total drops to 9 items

### Requirement: Trips are private to their family

A trip SHALL belong to exactly one family. Only members of that family SHALL be able to see the trip or change it and its items; for anyone else the trip SHALL be answered as if it does not exist.

#### Scenario: Trip list shows only the family's trips

- **WHEN** a member of "Jansen" opens the trip list
- **THEN** only trips of "Jansen" are listed

#### Scenario: Another family's trip is not found

- **WHEN** a member of "Jansen" opens or advances an item of a trip of "Verburgh" by its id
- **THEN** the system responds with not found and the trip is unchanged
