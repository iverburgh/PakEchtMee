# Spec Delta

## MODIFIED Requirements

### Requirement: Optional quantity and unit on a catalog item

The system SHALL allow a catalog item to optionally carry a quantity rule: a quantity, a basis, a period and a unit. When present, the quantity SHALL be an integer of at least 1 and at most 9999. The basis SHALL be one of fixed, per person, per adult or per child, and SHALL default to fixed. The period SHALL be one of per trip, per day or per night, and SHALL default to per trip. A unit SHALL be at most 20 characters and SHALL only be accepted when a quantity is present. An item without a quantity SHALL be treated as a plain item with no amount shown.

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

#### Scenario: Item with a quantity per child

- **WHEN** a user adds "Jassen" with quantity 1 and basis per child
- **THEN** the catalog shows "Jassen" with the rule "1 per kind"

#### Scenario: Quantity per person with a unit

- **WHEN** a user adds "Water" with quantity 2, unit "liter" and basis per person
- **THEN** the catalog shows "Water" with the rule "2 liter per persoon"

#### Scenario: Basis without quantity is rejected

- **WHEN** a user submits an item with basis per adult and no quantity
- **THEN** the system rejects the request with a validation error

#### Scenario: Quantity per child per day

- **WHEN** a user adds "Sokken" with quantity 1, basis per child and period per day
- **THEN** the catalog shows "Sokken" with the rule "1 per kind per dag"

#### Scenario: Fixed quantity per night

- **WHEN** a user adds "Theezakjes" with quantity 2, basis fixed and period per night
- **THEN** the catalog shows "Theezakjes" with the rule "2 per nacht"

#### Scenario: Period without quantity is rejected

- **WHEN** a user submits an item with period per day and no quantity
- **THEN** the system rejects the request with a validation error
