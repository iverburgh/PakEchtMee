# Spec Delta

## MODIFIED Requirements

### Requirement: Seeded shared items are per person

The seeded catalog SHALL give these items the rule 1 per person, without a unit.

- `Vermaak op locatie`: `Badhanddoeken`
- `Algemeen`: `Geldige paspoorten`
- `Kampeerkeuken`: `Borden`, `Bekers`, `Bestek`
- `Kamperen – Algemeen`: `Stoelen`

#### Scenario: Plates per person

- **WHEN** the category `Kampeerkeuken` is browsed after seeding
- **THEN** `Borden` has the rule 1 per person and `Pannen` has the fixed quantity 3

### Requirement: Seeded underwear and socks are per day

The seeded catalog SHALL give these items a quantity of 1 per day, without a unit:

- `Kleding`: `Onderbroeken` and `Sokken`, per adult
- `Kinderen – Kleding`: `Sokken`, per child

#### Scenario: Children's socks per day

- **WHEN** the category `Kinderen – Kleding` is browsed after seeding
- **THEN** `Sokken` has the rule 1 per child per day and `Truien` has the rule 2 per child

## ADDED Requirements

### Requirement: Every seeded item carries a quantity rule

Every seeded item SHALL carry a quantity rule: a quantity of at least 1, an optional unit, a basis and a period. Items not named in the per-child, per-adult, per-person or per-day requirements SHALL get a rule that fits a family packing list: a fixed number for shared gear, and a per-head or per-day rule for personal or daily items.

#### Scenario: No seeded item without an amount

- **WHEN** the catalog is seeded into an empty database
- **THEN** all 149 seeded items show an amount

#### Scenario: Shared camping gear is fixed

- **WHEN** the category `Tent` is browsed after seeding
- **THEN** `Haringen` has the fixed quantity 30 and `Tent + pomp` the fixed quantity 1

#### Scenario: Daily items per day

- **WHEN** the categories `Kleding` and `Kamperen – Algemeen` are browsed after seeding
- **THEN** `T-shirts` has the rule 1 per adult per day and `WC-papier` has the rule 1 rol per day

#### Scenario: Units where they help

- **WHEN** the category `Toiletartikelen` is browsed after seeding
- **THEN** `Shampoo` has the rule 1 fles

### Requirement: Remaining seeded items get their rule once on upgrade

On upgrade, each seeded general item that has no rule from the earlier upgrades SHALL get its seeded rule, but only while it still has no quantity and no unit. An item that was given an amount SHALL be left as it is. The upgrade SHALL run once per database. Existing trip items SHALL be unchanged.

#### Scenario: Untouched item is filled in

- **WHEN** the database contains the seeded `Haringen` without an amount, and the app is upgraded
- **THEN** `Haringen` has the fixed quantity 30

#### Scenario: Changed item is kept

- **WHEN** the administrator had given the seeded `Pannen` the quantity 2 before the upgrade
- **THEN** `Pannen` keeps the quantity 2 after the upgrade

#### Scenario: Existing trips are unchanged

- **WHEN** the trip `Kamperen Frankrijk 2026` exists before the upgrade
- **THEN** its trip items keep the amounts they had
