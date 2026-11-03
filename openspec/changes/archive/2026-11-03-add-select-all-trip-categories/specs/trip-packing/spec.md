# Spec Delta

## ADDED Requirements

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
