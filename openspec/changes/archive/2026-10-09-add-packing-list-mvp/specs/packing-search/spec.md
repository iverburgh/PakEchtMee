# Spec Delta

## Purpose

Lets a user find a specific item in a trip's packing list within seconds by searching across every category at once and by narrowing the list with category and status filters.

## ADDED Requirements

### Requirement: Search across all active items regardless of category

The system SHALL provide a search over the items of a trip that matches on the item name across all categories at once, without the user first selecting a category. Matching SHALL be case-insensitive, diacritics-insensitive and SHALL match any substring of the name. Results SHALL update automatically while typing, without the user submitting the query. `Deleted` items SHALL be excluded from results unless the user explicitly filters on status `Deleted`.

#### Scenario: Searching finds items from multiple categories

- **WHEN** a user types "char" in the search field
- **THEN** the list shows "Charger" from "Electronics" and "Charcoal" from "Barbecue", each with its category

#### Scenario: Case and diacritics are ignored

- **WHEN** a user types "kaarsen" and the trip contains "Käarsen"
- **THEN** the item is included in the results

#### Scenario: Search updates while typing

- **WHEN** a user adds another character to the query
- **THEN** the result list narrows without the user pressing a search or submit button

#### Scenario: Deleted items are excluded by default

- **WHEN** a user searches for an item name that only matches a `Deleted` item
- **THEN** the result list is empty and the deleted item is not shown

#### Scenario: Clearing the query

- **WHEN** a user clears the search field
- **THEN** the full list for the current filters is shown again

### Requirement: Filter by category and status

The system SHALL allow filtering a trip's items by one or more categories and by one or more statuses. Filters SHALL combine as an intersection across the two filter kinds and as a union within one filter kind. Active filters SHALL be visible and SHALL be clearable in a single action.

#### Scenario: Filtering by status

- **WHEN** a user filters on status `Active`
- **THEN** only items that still have to be prepared are listed

#### Scenario: Filtering by multiple categories

- **WHEN** a user filters on the categories "Toiletries" and "Electronics"
- **THEN** items from either category are listed and items from other categories are not

#### Scenario: Clearing all filters

- **WHEN** a user clears the filters
- **THEN** all non-deleted items of the trip are listed again

### Requirement: Search and filters combine

The system SHALL apply the search query and the active filters together, and SHALL report when the combination yields no results, including which filters are responsible.

#### Scenario: Query combined with a status filter

- **WHEN** a user searches "sock" with the status filter set to `Packed`
- **THEN** only items whose name matches "sock" and whose status is `Packed` are listed

#### Scenario: Empty result is explained

- **WHEN** the combination of query and filters matches no items
- **THEN** the system shows an empty-state message naming the active query and filters and offering to clear them
