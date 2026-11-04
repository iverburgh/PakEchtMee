# Proposal

## Why

On a trip's packing list you only see the item name, for example "Jassen". You cannot see how many to pack: one per person, one per child, or a fixed number. Today a catalog item can only carry a fixed quantity, such as "Slaapzak 3", which silently assumes three children. That number is wrong for another family, and for a trip where not everyone comes along. The family wants every item to say how many to take, derived from who is coming, with the option to deviate per trip.

## What Changes

- **Family composition**: a family records how many adults and how many children it has. Every member of the family can change this on a new "Gezin" page. Existing families start with 2 adults and 0 children; the migration sets "Verburgh" to 2 adults and 3 children.
- **Quantity rule on a catalog item**: the optional quantity becomes a quantity rule. A rule is a count with a basis:
  - fixed (as today);
  - per person;
  - per adult;
  - per child.
  
  The unit stays optional. The catalog shows the rule, for example "1 per kind" or "2 liter".
- **Trip composition**: a trip gets its own number of adults and children. It is copied from the family when the trip is created, and any member can change it per trip.
- **Trip item quantity**: when a category is added to a trip, each trip item copies the rule from the catalog. Its total is calculated from the trip's composition, for example 1 per kind × 3 children = 3. Changing the trip's composition recalculates the totals of the items in status `Active` ("Te doen"). Items that have progressed further keep their total.
- **Change a quantity on a trip**: a member can change the rule of a single trip item, including its count, basis and unit, or remove it. This is only allowed while the item is `Active`. The total follows the trip's composition.
- **Display**: in the walkthrough and the overview, each item shows its total prominently, for example "3". Next to it, the rule appears in a colour per basis: person, adult, child, or fixed.
- **Seeded catalog**:
  - The seeded items with "3x" become "1 per kind".
  - A list of clothing, sleeping and tableware items gets a per-person, per-adult or per-child rule (see the `development-seed-data` delta).
  - A one-off migration updates these general items in existing databases, but only when they still have their seeded amount.
- **API**:
  - Catalog and trip item payloads gain a `basis` field next to `quantity` and `unit`.
  - Trip items additionally carry a calculated `total`.
  
  Clients that ignore `basis` keep working, because `basis` defaults to fixed.

## Capabilities

### New Capabilities

- `family-composition`: a family's number of adults and children, maintained by its members, used as the default for new trips.

### Modified Capabilities

- `packing-catalog`: "Optional quantity and unit on a catalog item" becomes a quantity rule with a basis.
- `trip-packing`:
  - "Create a trip" copies the family composition onto the trip.
  - "Select categories to generate the trip list" copies the rule and calculates the total.
  - New requirements cover:
    - changing the trip composition with recalculation;
    - changing a trip item's quantity while `Active`;
    - showing the total and the coloured rule.
- `development-seed-data`: "Multiplied items carry a quantity" becomes per-child rules, plus a requirement for the per-person rules and the one-off update of existing seeded items.

## Impact

- **Families context**:
  - `Family` gets a `FamilyComposition` value object.
  - A new handler to change it.
  - An EF migration adding `Adults`/`Children` columns and setting "Verburgh" to 2 + 3.
- **Catalog context**:
  - `Amount` gains a `QuantityBasis`.
  - Create and update item handlers and DTOs carry the basis.
  - An EF migration adds the `QuantityBasis` column.
  - A data update converts the seeded items that are still untouched.
- **Trips context**:
  - `Trip` gets a `TripComposition`.
  - `TripItem` stores rule and total.
  - New operations to change the trip composition and a trip item's quantity.
  - An EF migration adds the new columns. Existing trips get 2 adults and 0 children, and existing items get basis fixed with total = quantity.
- **Web (composition root)**:
  - Trip creation reads the family composition and passes it to the trip.
  - New endpoints: `GET/PUT /api/family/composition`, `PUT /api/trips/{id}/composition`, `PUT /api/trips/{id}/items/{itemId}/quantity`.
- **Frontend**:
  - New "Gezin" page.
  - The catalog item dialog gets a basis choice.
  - Trip page: a composition summary with an edit dialog, a quantity display with a coloured rule chip, and a quantity dialog on `Active` items.
  - The overview shows total and rule.
- **Tests**:
  - Domain and handler unit tests in all three contexts.
  - Repository integration tests for the new columns.
  - Web tests for the new endpoints and the access rules.
- **Dependencies**: none.
