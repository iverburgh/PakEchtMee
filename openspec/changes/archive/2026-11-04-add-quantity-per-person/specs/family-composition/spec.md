# Spec Delta

## Purpose

Records how many adults and children a family consists of, so packing quantities can be expressed per person, per adult or per child and new trips start from the family's real size.

## ADDED Requirements

### Requirement: A family has a composition

Every family SHALL have a number of adults and a number of children. Each number SHALL be an integer from 0 to 20, and together they SHALL be at least 1. A family created without a composition SHALL start with 2 adults and 0 children.

#### Scenario: New family gets the default composition

- **WHEN** the administrator creates the family "Jansen"
- **THEN** "Jansen" has 2 adults and 0 children

#### Scenario: Empty family is rejected

- **WHEN** a member sets the composition to 0 adults and 0 children
- **THEN** the request is rejected with a validation error and the composition is unchanged

#### Scenario: Out-of-range number is rejected

- **WHEN** a member sets the number of children to 21 or to -1
- **THEN** the request is rejected with a validation error and the composition is unchanged

### Requirement: Members maintain their family's composition

Every member of a family SHALL be able to view and change the composition of their own family. A change SHALL NOT alter any existing trip. An account that is not a member of a family SHALL get status 403 when it reads or changes a composition.

#### Scenario: Member changes the composition

- **WHEN** a member of "Verburgh" sets the composition to 2 adults and 3 children
- **THEN** "Verburgh" has 2 adults and 3 children, and its existing trips keep their own composition

#### Scenario: Administrator without a family

- **WHEN** an administrator who is not a member of any family opens the composition
- **THEN** the request is rejected with status 403

### Requirement: Existing families get a composition

On upgrade, every existing family SHALL get 2 adults and 0 children, except "Verburgh", which SHALL get 2 adults and 3 children.

#### Scenario: Verburgh after the upgrade

- **WHEN** the upgraded app is opened by a member of "Verburgh"
- **THEN** the family page shows 2 adults and 3 children
