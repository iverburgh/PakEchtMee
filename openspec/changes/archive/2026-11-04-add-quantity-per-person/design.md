# Design

## Context

See proposal.md (Why). The current state that shapes the approach:

- **Amount types.** Catalog and Trips each have their own `Amount` value object (`Quantity` 1–9999 and an optional `Unit` of up to 20 characters). They are deliberately separate types so the bounded contexts stay independent. `CatalogItemSnapshot(SourceItemId, Name, Quantity, Unit)` carries the amount from the catalog to a trip.
- **Composition root.** `src/Web` is the only place that sees more than one context. `CategorySelections.ReadAsync` reads the catalog through MediatR and builds the snapshots, and `DevelopmentSeeder` uses the same path. Trips never reads Catalog or Families itself.
- **Families.** `Family` only has a name and its members, which are login accounts. All of its handlers are administrator-only. `CurrentUser.FamilyId` identifies a member's family for every member endpoint.
- **Trips.** `Trip` is the aggregate and every `TripItem` change goes through it. Status transitions live in `TripItem.ChangeStatus`.
- **Seeding.**
  - The catalog seed (`CatalogSeedData`) runs in every environment and only inserts missing records. Its ids are deterministic (`SeedId.For("item:{category}/{name}")`).
  - Production already contains the seeded general items.
- **Frontend.**
  - Amounts are rendered with `describeAmount(quantity, unit)` in the trip workspace, the overview and the catalog item list.
  - The catalog item dialog uses react-hook-form and zod.
- **Tests.**
  - Handler tests use Moq through a per-context harness, for example `TripsHarness`.
  - Web tests use `ApiFixture` with `TestAuthenticationHandler`.
  - Repository tests use Testcontainers.

## Goals / Non-Goals

**Goals:**

- One rule model (quantity × basis) in both Catalog and Trips. Each context still owns its own type.
- The total a member sees for an item that has progressed never changes behind their back.
- Existing API clients keep working: `basis` is optional on input and defaults to fixed.

**Non-Goals:**

- No named people or per-person checklists. A composition is just two counts.
- No rounding rules or fractions ("1 per 2 people"). Quantity × count is enough for this family.
- Changing the family composition does not touch existing trips (spec). There is no "apply to all trips" action.
- The administrator family page does not get a composition editor. Members maintain it themselves.

## Decisions

### D1. `QuantityBasis` extends the existing `Amount` in both contexts

Each context gets an enum `QuantityBasis { Fixed, PerPerson, PerAdult, PerChild }`, and `Amount` gains a `Basis` property.

- `Amount.Create(int? quantity, string? unit, QuantityBasis basis = Fixed)` keeps all existing validation, and rejects a non-fixed basis without a quantity.
- `ToString()` renders the rule in Dutch:
  - fixed: `2 liter`;
  - per child: `1 per kind`;
  - per person with a unit: `2 liter per persoon`.
- The basis is persisted as a string column `QuantityBasis` (`HasConversion<string>()`, max 20, default `'Fixed'`). That keeps the database readable, in line with how `Status` is stored.
- `CatalogItemSnapshot` gains `QuantityBasis Basis`.

**Alternative:** a separate `QuantityRule` type next to `Amount`. Rejected, because `Amount` already *is* the rule and the "unit only with quantity" invariant would have to be duplicated.

### D2. Composition is a value object per context

- **Families:** `FamilyComposition(int Adults, int Children)`.
  - Each count is 0–20 and the total is at least 1, guarded by `DomainValidationException` with Dutch messages.
  - `Family.ChangeComposition(FamilyComposition)`.
  - `Family.Create` defaults to `(2, 0)`.
- **Trips:** `TripComposition(int Adults, int Children)` with the same rules.
  - Its method `Multiplier(QuantityBasis)` returns: Fixed → 1, PerPerson → adults + children, PerAdult → adults, PerChild → children.
- Both are mapped as owned value objects (columns `Adults`, `Children`).

**Why two types:** the same reason as `Amount`. Trips must not depend on Families.Domain.

### D3. A trip item stores its rule and its calculated total

`TripItem` keeps `Amount` (the rule) and gets a persisted `int? Total`.

- `Trip` calculates the total when an item is created (`SelectCategories`), when its rule changes, and when the composition changes.
- **Why persisted:** the spec says progressed items keep their total when the composition changes. A total that is always derived from the current composition would silently change for `Packed` items.
- The DTO exposes `quantity`, `unit`, `basis` and `total`. The overview and walkthrough display `total` and the rule.

### D4. New trip operations live on the aggregate

- `Trip.ChangeComposition(TripComposition composition)`
  - Sets the composition.
  - Recalculates `Total` for every non-deleted `Active` item whose basis is not `Fixed`.
- `Trip.ChangeItemAmount(Guid itemId, Amount? amount)`
  - Only allowed when the item is `Active` and not deleted. Otherwise it throws `DomainValidationException("'Jassen' is al opgepakt; het aantal kan alleen bij Te doen worden gewijzigd.")`.
  - Recalculates that item's `Total`.
- `Trip.Create(..., TripComposition? composition = null)` takes an optional composition that defaults to 2 adults and 0 children, so existing callers and tests keep compiling. `CreateTripHandler.Command` gets required `Adults` and `Children`, and the seeder always passes the family's composition, so the default only covers tests.

Handlers:

- `ChangeTripCompositionHandler` returns `TripCompositionChangedDto(Trip, ChangedTotals)`: the trip details, because many items can change at once, plus the number of recalculated totals for the toast.
- `ChangeTripItemAmountHandler` returns `TripItemDto`.

### D5. The composition root passes the family composition to Trips

- **Families.Application:**
  - `GetFamilyCompositionHandler.Query(FamilyId)` → `FamilyCompositionDto(Adults, Children)`.
  - `ChangeFamilyCompositionHandler.Command(FamilyId, Adults, Children)`.
- **Trip creation:** the `POST /api/trips` endpoint first sends `GetFamilyCompositionHandler.Query(user.FamilyId)`, then `CreateTripHandler.Command(..., adults, children)`.
- **Seeding:** `DevelopmentSeeder` does the same for the seeded trips, through `TripSeed`.
- **Alternative:** have the client send the composition with `POST /api/trips`. Rejected, because the spec says a new trip *takes over* the family composition. The server decides, so the client cannot get out of sync.

### D6. Endpoints and access

| Endpoint | Purpose | Access |
| --- | --- | --- |
| `GET /api/family/composition` | The member's own family composition | Family member (same filter as trips); 403 without a family |
| `PUT /api/family/composition` `{ adults, children }` | Change it | Family member |
| `PUT /api/trips/{tripId}/composition` `{ adults, children }` | Change a trip's composition; returns trip details | Family member; a foreign trip is 404 (existing `FindAsync(tripId, familyId)`) |
| `PUT /api/trips/{tripId}/items/{itemId}/quantity` `{ quantity, unit, basis }` | Change or clear (`quantity: null`) an item's rule | Family member |

`POST/PUT /api/items` (catalog) accept an optional `basis`. Missing means `Fixed`, and the API returns it on every catalog item and trip item.

### D7. Migrations, one per context

- **Families `AddFamilyComposition`:**
  - Adds `Adults int NOT NULL DEFAULT 2` and `Children int NOT NULL DEFAULT 0`.
  - Runs `UPDATE … SET Children = 3 WHERE Id = KnownFamilies.Verburgh`.
- **Catalog `AddQuantityBasis`:**
  - Adds `QuantityBasis nvarchar(20) NULL`, then sets `'Fixed'` wherever a quantity exists. It has to be nullable because `Amount` is an optional owned type: EF writes NULL to all of its columns for an item without an amount.
  - A data step updates each seeded item from the spec lists, matched on its deterministic id (`CatalogSeedData.ItemId(category, name)`, computed when the migration is written and inlined as literal GUIDs).
  - The update sets `Quantity = 1, QuantityBasis = '<basis>'`, but only for items in a general category (`FamilyId IS NULL` on the category) with `Unit IS NULL` and their exact seeded quantity: `Quantity = 3` for the former "3x" items, `Quantity IS NULL` for all others. That condition is what "still untouched" means.
  - **Why literals:** a migration must stay the same if `CatalogSeedData` is renamed later.
- **Trips `AddQuantityPerPerson`:**
  - Adds `Adults int NOT NULL DEFAULT 2` and `Children int NOT NULL DEFAULT 0` on trips.
  - Adds `QuantityBasis nvarchar(20) NULL` (nullable for the same reason as in the catalog) and `Total int NULL` on trip items.
  - Runs `UPDATE TripItems SET QuantityBasis = 'Fixed', Total = Quantity WHERE Quantity IS NOT NULL`, so existing items keep exactly what they showed.
- **Seed data:** `CatalogSeedData.Item` gets an optional basis, and the "3x" items become `(1, PerChild)`. Fresh databases then match the migrated ones.

### D8. Frontend

- **Shared helpers in `lib/types.ts`:**
  - `QuantityBasis` type and labels: "vast", "per persoon", "per volwassene", "per kind".
  - `describeRule(quantity, unit, basis)`.
  - A `basisTone` map with chip classes per basis: person = teal, adult = violet, child = rose, all with dark-mode variants.
  - **Why these colours:** the status badges already use slate, amber, sky and emerald, so these never look like a status. They are readable in both themes.
- **`QuantityBadge` and `RuleChip` components:** the badge shows the total in a bold rounded pill (unit after it), plus the rule chip for non-fixed bases. The walkthrough card uses the badge, and the catalog item list uses the chip alone, because the catalog has no total. The overview shows the total in bold and the rule as plain text in brackets, so a black-and-white print stays readable.
- **Walkthrough (`trip-workspace.tsx`):**
  - For `Active` items the name and badge together form a button that opens a `QuantityDialog`. This keeps it reachable for items that have no quantity yet. The dialog uses react-hook-form + zod with quantity, unit, and basis as a single-choice toggle group; an empty quantity means "geen aantal".
  - For other statuses it is plain text.
  - After saving, the server action revalidates the trip page and a Sonner toast shows the new total.
  - The quantity, unit and basis fields and their zod rules live in a shared `QuantityFields` component, which the catalog dialog uses too.
- **Trip header:** shows "2 volwassenen · 3 kinderen" as a button that opens `CompositionDialog`, a − / + stepper form shared with the Gezin page. On save, the server action revalidates the trip page and a toast says how many totals changed.
- **`/gezin` page (Server Component plus a small client form):** shows and edits the family composition. It is linked from the Account menu in the bottom navigation, for members with a family. A fifth tab would be too cramped at phone width.
- **Catalog item dialog:** gets the basis radio group under quantity and unit.

## Risks / Trade-offs

- [The migration's data step converts an item that the administrator deliberately left at 3] → The condition only matches the exact seeded values, and the result ("1 per kind" with 3 children) still totals 3 for Verburgh. The effect is nil for that family.
- [Old trips get 2 adults and 0 children, while Verburgh has 3 children] → Their existing items keep a fixed basis and their total, so nothing changes visibly. Items added later use the trip's composition, which a member can fix in one dialog. This is accepted rather than joining across contexts in a migration.
- [A total of 0 (per child on a trip without children) looks odd] → The spec keeps it visible on purpose, so the member can delete or change it. It is not hidden.
- [More information on a small card] → The rule chip is short ("1 per kind"), and on phone width it wraps under the name rather than squeezing the actions.

## Migration Plan

1. Deploy as usual: the push to `main` runs verify, then deploy. The API applies the three migrations at startup (existing `production-hosting` behaviour). After that, the catalog startup seeder only adds missing items.
2. **Rollback:** revert the commit and redeploy. The added columns are nullable or have defaults, so the previous code ignores them. A down-migration is not needed for rollback.
