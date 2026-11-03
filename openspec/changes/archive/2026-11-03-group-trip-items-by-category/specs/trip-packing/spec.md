# Spec Delta

## ADDED Requirements

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
