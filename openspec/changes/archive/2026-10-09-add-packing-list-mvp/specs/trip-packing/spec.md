# Spec Delta

## Purpose

Covers a trip (a holiday or a day trip): choosing which catalog categories to take, generating the trip's packing list from them, and moving every trip item through its packing lifecycle quickly and reliably.

## ADDED Requirements

### Requirement: Create a trip

The system SHALL allow a user to create a trip with a non-empty name of at most 100 characters and an optional start date and end date. When both dates are given, the end date SHALL NOT be earlier than the start date. Trips SHALL be listed with the most relevant (upcoming or ongoing) trip first.

#### Scenario: Create a trip with dates

- **WHEN** a user creates the trip "Summer France" with start 2026-07-04 and end 2026-07-18
- **THEN** the trip is stored and appears in the trip list

#### Scenario: Create a trip without dates

- **WHEN** a user creates the trip "Day at the beach" without dates
- **THEN** the trip is stored and appears in the trip list

#### Scenario: End date before start date is rejected

- **WHEN** a user submits a trip whose end date precedes its start date
- **THEN** the system rejects the request with a validation error and no trip is created

### Requirement: Select categories to generate the trip list

The system SHALL let a user select one or more non-removed categories for a trip. On selection, the system SHALL create one trip item for every non-removed catalog item in each selected category, copying the item name, category name, quantity and unit at that moment. Every generated trip item SHALL start in status `Active`. Adding a category later SHALL add only the trip items that do not yet exist for that trip.

#### Scenario: Generating the list from selected categories

- **WHEN** a user selects "Toiletries" (4 items) and "Electronics" (3 items) for a trip
- **THEN** the trip contains 7 trip items, all with status `Active`

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

### Requirement: Trip progress

The system SHALL show, per trip, how many non-deleted items are in each status and the share of items that reached `Loaded`. The counts SHALL update immediately after every status change.

#### Scenario: Progress after advancing items

- **WHEN** a trip has 10 non-deleted items of which 4 are `Loaded`
- **THEN** the trip shows 4 of 10 loaded together with the counts of the other statuses

#### Scenario: Deleted items excluded from progress

- **WHEN** a user deletes an `Active` item from a trip of 10 items
- **THEN** the progress total drops to 9 items
