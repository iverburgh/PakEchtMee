# Spec Delta

## MODIFIED Requirements

### Requirement: Manage categories

The system SHALL allow a family member to create, rename and remove the family's own categories, and the administrator to do the same for general categories. A category SHALL have a name that is non-empty, at most 60 characters, and unique among the non-removed categories of the same scope - one family, or the general catalog - compared case-insensitively. Removal SHALL be a soft delete: a removed category and its items SHALL no longer appear in catalog browsing or category selection, and removal SHALL be reversible.

#### Scenario: Create a category

- **WHEN** a member of "Verburgh" creates a category with the name "Toiletries"
- **THEN** the category is stored as an own category of "Verburgh" and appears in its catalog and in the category selection of its new trips

#### Scenario: Duplicate category name is rejected

- **WHEN** a member of "Verburgh" creates a category named "toiletries" while "Verburgh" has a non-removed category "Toiletries"
- **THEN** the system rejects the request with a validation error naming the conflict, and no category is created

#### Scenario: Same name in another scope is allowed

- **WHEN** a member of "Jansen" creates "Toiletries" while "Verburgh" has an own category "Toiletries" and the general catalog has one too
- **THEN** the category is created for "Jansen" and all three exist independently

#### Scenario: Empty category name is rejected

- **WHEN** a user submits a category with a blank name
- **THEN** the system rejects the request with a validation error and no category is created

#### Scenario: Removing a category hides it from selection

- **WHEN** a member removes the own category "Ski gear"
- **THEN** the category and its items are no longer offered when selecting categories for a new trip of that family, and already existing trips are unaffected

#### Scenario: Restoring a removed category

- **WHEN** a member restores a previously removed own category
- **THEN** the category and its non-removed items are available again, provided no other non-removed category of the family holds the same name

### Requirement: Catalog item belongs to exactly one category

The system SHALL allow a user to create, rename, move and remove catalog items in categories they may edit: family members in their family's own categories, the administrator in general categories. Every catalog item SHALL belong to exactly one non-removed category and an item SHALL only move between categories of the same scope. An item name SHALL be non-empty, at most 100 characters, and unique within its category (compared case-insensitively). Removal SHALL be a soft delete and reversible.

#### Scenario: Create an item in a category

- **WHEN** a member adds the item "Toothbrush" to the own category "Toiletries"
- **THEN** the item is stored under that category and is listed when browsing that category

#### Scenario: Duplicate item name within a category is rejected

- **WHEN** a member adds "toothbrush" to "Toiletries" while a non-removed item "Toothbrush" exists in that category
- **THEN** the system rejects the request with a validation error and no item is created

#### Scenario: Same item name in another category is allowed

- **WHEN** a member adds "Charger" to "Electronics" while "Charger" already exists in "Bathroom"
- **THEN** both items exist independently

#### Scenario: Move an item to another category

- **WHEN** a member moves "Charger" from the own category "Electronics" to the own category "Travel documents"
- **THEN** the item is listed under "Travel documents" and no longer under "Electronics", and existing trip lists are unaffected

#### Scenario: Item cannot move between scopes

- **WHEN** the administrator tries to move a general item into an own category of "Verburgh"
- **THEN** the system rejects the request with a validation error

#### Scenario: Item cannot be created in a removed category

- **WHEN** a user tries to add an item to a removed category
- **THEN** the system rejects the request with a validation error

## ADDED Requirements

### Requirement: Catalog data is private to its family

A family's own categories and items SHALL only be visible to and changeable by members of that family. A request for an own category or item of another family SHALL be answered as if it does not exist.

#### Scenario: Another family's category is not found

- **WHEN** a member of "Jansen" requests an own category of "Verburgh" by its id
- **THEN** the system responds with not found

#### Scenario: Catalog only lists the family's data

- **WHEN** a member of "Jansen" opens the catalog
- **THEN** no own category of "Verburgh" is listed

### Requirement: General catalog maintained by the administrator

General categories and their items SHALL belong to no family and SHALL be created, renamed, moved, removed and restored only by the administrator. Family members SHALL see the general categories they have switched on, marked as general, and SHALL NOT be able to change them or add items to them.

#### Scenario: Member cannot edit a general category

- **WHEN** a member of "Verburgh" who is not an administrator tries to rename the general category "Tent"
- **THEN** the request is rejected with status 403 and the name is unchanged

#### Scenario: Member cannot add an item to a general category

- **WHEN** a member tries to add "Extra haringen" to the general category "Tent"
- **THEN** the request is rejected with status 403

#### Scenario: Administrator changes show up for every family

- **WHEN** the administrator adds "Hamer" to the general category "Tent"
- **THEN** every family that has "Tent" switched on sees "Hamer" in it, and existing trips are unaffected

### Requirement: Family switches general categories on and off

Each family SHALL be able to switch every non-removed general category off and on again. A general category SHALL be on for a family unless that family switched it off, so a new general category is on for every family. A switched-off category SHALL NOT be listed in the family's catalog or offered for its new trips; existing trips SHALL be unaffected.

#### Scenario: Switching a general category off

- **WHEN** a member of "Jansen" switches the general category "Kampeerkeuken" off
- **THEN** "Kampeerkeuken" no longer appears in the catalog of "Jansen" or in its trip category selection, while "Verburgh" still sees it

#### Scenario: Switching it back on

- **WHEN** a member of "Jansen" switches "Kampeerkeuken" on again
- **THEN** the category and its current items are available to "Jansen" again

#### Scenario: New general category appears everywhere

- **WHEN** the administrator creates the general category "Skispullen"
- **THEN** it is on for every family until a family switches it off
