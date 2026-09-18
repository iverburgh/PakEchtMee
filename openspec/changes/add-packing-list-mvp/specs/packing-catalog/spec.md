# Spec Delta

## Purpose

The reusable master data of the packing app: categories and the catalog items that belong to them, which together form the source from which every trip packing list is generated.

## ADDED Requirements

### Requirement: Manage categories

The system SHALL allow a user to create, rename and remove categories. A category SHALL have a name that is non-empty, at most 60 characters, and unique among non-removed categories (compared case-insensitively). Removal SHALL be a soft delete: a removed category and its items SHALL no longer appear in catalog browsing or category selection, and removal SHALL be reversible.

#### Scenario: Create a category

- **WHEN** a user creates a category with the name "Toiletries"
- **THEN** the category is stored and appears in the catalog and in the category selection of a new trip

#### Scenario: Duplicate category name is rejected

- **WHEN** a user creates a category named "toiletries" while a non-removed category "Toiletries" exists
- **THEN** the system rejects the request with a validation error naming the conflict, and no category is created

#### Scenario: Empty category name is rejected

- **WHEN** a user submits a category with a blank name
- **THEN** the system rejects the request with a validation error and no category is created

#### Scenario: Removing a category hides it from selection

- **WHEN** a user removes the category "Ski gear"
- **THEN** the category and its items are no longer offered when selecting categories for a new trip, and already existing trips are unaffected

#### Scenario: Restoring a removed category

- **WHEN** a user restores a previously removed category
- **THEN** the category and its non-removed items are available again, provided no other non-removed category holds the same name

### Requirement: Catalog item belongs to exactly one category

The system SHALL allow a user to create, rename, move and remove catalog items. Every catalog item SHALL belong to exactly one non-removed category. An item name SHALL be non-empty, at most 100 characters, and unique within its category (compared case-insensitively). Removal SHALL be a soft delete and SHALL be reversible.

#### Scenario: Create an item in a category

- **WHEN** a user adds the item "Toothbrush" to the category "Toiletries"
- **THEN** the item is stored under that category and is listed when browsing that category

#### Scenario: Duplicate item name within a category is rejected

- **WHEN** a user adds "toothbrush" to "Toiletries" while a non-removed item "Toothbrush" exists in that category
- **THEN** the system rejects the request with a validation error and no item is created

#### Scenario: Same item name in another category is allowed

- **WHEN** a user adds "Charger" to "Electronics" while "Charger" already exists in "Bathroom"
- **THEN** both items exist independently

#### Scenario: Move an item to another category

- **WHEN** a user moves "Charger" from "Electronics" to "Travel documents"
- **THEN** the item is listed under "Travel documents" and no longer under "Electronics", and existing trip lists are unaffected

#### Scenario: Item cannot be created in a removed category

- **WHEN** a user tries to add an item to a removed category
- **THEN** the system rejects the request with a validation error

### Requirement: Optional quantity and unit on a catalog item

The system SHALL allow a catalog item to optionally carry a quantity and a unit. When present, the quantity SHALL be an integer of at least 1 and at most 9999. A unit SHALL be at most 20 characters and SHALL only be accepted when a quantity is present. An item without a quantity SHALL be treated as a plain item with no amount shown.

#### Scenario: Item with quantity and unit

- **WHEN** a user adds "Socks" with quantity 7 and unit "pair"
- **THEN** the item is shown as "Socks 7 pair" wherever the item is displayed

#### Scenario: Item with quantity but no unit

- **WHEN** a user adds "Towel" with quantity 2 and no unit
- **THEN** the item is shown as "Towel 2"

#### Scenario: Item without quantity

- **WHEN** a user adds "Passport" without a quantity
- **THEN** the item is shown as "Passport" with no amount

#### Scenario: Unit without quantity is rejected

- **WHEN** a user submits an item with unit "pair" and no quantity
- **THEN** the system rejects the request with a validation error

#### Scenario: Non-positive quantity is rejected

- **WHEN** a user submits an item with quantity 0 or a negative quantity
- **THEN** the system rejects the request with a validation error
