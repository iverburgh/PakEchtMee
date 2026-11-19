# Spec Delta

## MODIFIED Requirements

### Requirement: Seeded shared items are per person

The seeded catalog SHALL give these items the rule 1 per person, without a unit. Every seeded item not named in this, the per-child, the per-adult or the per-day lists SHALL stay without a quantity.

- `Vermaak op locatie`: `Badhanddoeken`
- `Algemeen`: `Geldige paspoorten`
- `Kampeerkeuken`: `Borden`, `Bekers`, `Bestek`
- `Kamperen – Algemeen`: `Stoelen`

#### Scenario: Plates per person

- **WHEN** the category `Kampeerkeuken` is browsed after seeding
- **THEN** `Borden` has the rule 1 per person and `Pannen` has no quantity

## ADDED Requirements

### Requirement: Seeded underwear and socks are per day

The seeded catalog SHALL give these items a quantity of 1 per day, without a unit:

- `Kleding`: `Onderbroeken` and `Sokken`, per adult
- `Kinderen – Kleding`: `Sokken`, per child

#### Scenario: Children's socks per day

- **WHEN** the category `Kinderen – Kleding` is browsed after seeding
- **THEN** `Sokken` has the rule 1 per child per day and `Truien` has no quantity

### Requirement: Untouched clothing gets its per-day rule on upgrade

On upgrade, each seeded general item from the per-day list SHALL get its rule only while it still has no quantity and no unit. An item that was given an amount SHALL be left as it is. Existing trip items SHALL be unchanged, and every existing rule SHALL count per trip.

#### Scenario: Untouched socks are converted

- **WHEN** the database contains the seeded `Kleding` item `Sokken` without an amount, and the app is upgraded
- **THEN** `Sokken` has the rule 1 per adult per day

#### Scenario: Changed underwear is kept

- **WHEN** the administrator had given the seeded `Onderbroeken` the quantity 10 before the upgrade
- **THEN** `Onderbroeken` keeps the fixed quantity 10 per trip after the upgrade
