# Spec Delta

## Purpose

Provides a full-screen, category-grouped view of a trip's complete packing list that can be scanned at a glance and printed on paper as a checklist.

## ADDED Requirements

### Requirement: Full-screen overview of all trip items

The system SHALL provide a full-screen overview of a trip that lists all its items grouped by category, showing per item the name, the quantity and unit when present, and the current status. The overview SHALL show the trip name, the trip dates when present, and the per-status counts. `Deleted` items SHALL be excluded from the overview.

#### Scenario: Opening the overview

- **WHEN** a user opens the overview of a trip
- **THEN** all non-deleted items are shown grouped per category, each with its amount when present and its status

#### Scenario: Deleted items are omitted

- **WHEN** a trip contains deleted items
- **THEN** those items do not appear in the overview and are not counted

#### Scenario: Overview of an empty trip

- **WHEN** a user opens the overview of a trip without items
- **THEN** the overview shows an empty state inviting the user to select categories

### Requirement: Overview reflects the active filters

The system SHALL apply the active category and status filters to the overview and SHALL state on the overview which filters are applied, so that a partially filtered overview is never mistaken for the complete list.

#### Scenario: Filtered overview

- **WHEN** a user opens the overview while filtering on status `Active`
- **THEN** only items with status `Active` are shown and the overview states that this filter is applied

### Requirement: Print-friendly output

The system SHALL allow the overview to be printed from the browser. The printed output SHALL contain the trip name, the trip dates when present, the applied filters, the items grouped per category with their amount and status, and a checkbox per item. The printed output SHALL NOT contain navigation, buttons, filter controls or other interactive chrome, SHALL use a layout that fits standard A4 portrait width, SHALL avoid splitting a category heading from its first item across a page break, and SHALL remain legible in black and white without relying on colour alone to convey status.

#### Scenario: Printing the overview

- **WHEN** a user triggers print from the overview
- **THEN** the print preview shows the trip heading and the category-grouped items with a checkbox per item, and no navigation or buttons

#### Scenario: Status readable without colour

- **WHEN** the overview is printed in black and white
- **THEN** the status of each item is still identifiable from text or a symbol rather than from colour alone

#### Scenario: Multi-page list

- **WHEN** a trip has more items than fit on one page
- **THEN** the output continues on following pages without a category heading being left alone at the bottom of a page
