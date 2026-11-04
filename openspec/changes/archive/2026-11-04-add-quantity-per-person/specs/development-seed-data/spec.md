# Spec Delta

## MODIFIED Requirements

### Requirement: Multiplied items carry a quantity

A sheet entry written as `<name> <n>x` in a `Kinderen – …` category SHALL be seeded as an item named `<name>` with quantity 1 per child and no unit, because the multiplier on the sheet is the number of children.

#### Scenario: Sleeping bags for three children

- **WHEN** the category `Kinderen – Slapen` is browsed
- **THEN** it contains `Slaapzak`, `Matje` and `Kussen`, each with the rule 1 per child

## ADDED Requirements

### Requirement: Seeded children's items are per child

The seeded catalog SHALL give these items the rule 1 per child, without a unit:

- `Kinderen – Toiletartikelen`: `Handdoek(en) douchen`
- `Kinderen – Buitenkleding`: `Jassen`, `Laarzen`, `Schoenen`, `Crocs/sandalen/slippers`, `Petten`, `Zonnebrillen`
- `Kinderen – Vermaak op locatie`: `Zwemkleding`, `Handdoek(en) zwemmen`

#### Scenario: Jackets per child

- **WHEN** the category `Kinderen – Buitenkleding` is browsed after seeding
- **THEN** `Jassen` has the rule 1 per child

### Requirement: Seeded adult items are per adult

The seeded catalog SHALL give these items the rule 1 per adult, without a unit:

- `Buitenkleding`: `Jas`, `Slippers/crocs/teva's`, `Pet`, `Zonnebrillen`, `Wandelschoenen`
- `Toiletartikelen`: `Handdoeken douchen`
- `Slapen in tent`: `Matjes`, `Kussens`

#### Scenario: Adult jacket per adult

- **WHEN** the category `Buitenkleding` is browsed after seeding
- **THEN** `Jas` has the rule 1 per adult

### Requirement: Seeded shared items are per person

The seeded catalog SHALL give these items the rule 1 per person, without a unit. Every seeded item not named in this or the per-child or per-adult lists SHALL stay without a quantity.

- `Vermaak op locatie`: `Badhanddoeken`
- `Algemeen`: `Geldige paspoorten`
- `Kampeerkeuken`: `Borden`, `Bekers`, `Bestek`
- `Kamperen – Algemeen`: `Stoelen`

#### Scenario: Plates per person

- **WHEN** the category `Kampeerkeuken` is browsed after seeding
- **THEN** `Borden` has the rule 1 per person and `Pannen` has no quantity

### Requirement: Untouched seeded items get their rule on upgrade

On upgrade, each seeded general item from the per-child, per-adult and per-person lists SHALL get its rule only while it still has its originally seeded amount: no unit, and either no quantity or the seeded quantity 3. An item whose amount was changed SHALL be left as it is. Existing trip items SHALL be unchanged.

#### Scenario: Untouched item is converted

- **WHEN** the database contains the seeded `Slaapzak` with quantity 3 and no unit, and the app is upgraded
- **THEN** `Slaapzak` has the rule 1 per child

#### Scenario: Changed item is kept

- **WHEN** the administrator had given the seeded `Jassen` the quantity 4 before the upgrade
- **THEN** `Jassen` keeps the fixed quantity 4 after the upgrade

#### Scenario: Existing trips are unchanged

- **WHEN** the trip `Kamperen Frankrijk 2026` exists before the upgrade
- **THEN** its trip items keep their quantities, with a fixed basis

### Requirement: Seeded trips use the family composition

Seeded trips SHALL be created with the composition of their family at seeding time.

#### Scenario: Seeded camping trip on a fresh database

- **WHEN** a fresh development database is seeded and "Verburgh" has 2 adults and 3 children
- **THEN** `Kamperen Frankrijk 2026` has 2 adults and 3 children, and its `Slaapzak` item has a total of 3
