# Design

## Context

See proposal.md - Why. The repository is empty apart from `openspec/` and `.github/`, so this change establishes the entire solution skeleton as well as the first feature set. The technical frame is fixed by `.github/copilot-instructions.md`: .NET with DDD bounded contexts, MediatR vertical slices returning `Result<T, Exception>`, EF Core on SQL Server, .NET Aspire for local orchestration, and a Next.js App Router frontend with Tailwind v4 and shadcn/ui. The dominant usage context is a phone held in one hand, often with a poor mobile connection, while walking past cupboards - which drives several decisions below towards fewer round trips and forgiving interactions.

Constraints taken as given: single user, no authentication, no offline data sync (recorded as assumptions in the proposal).

## Goals / Non-Goals

**Goals:**

- A solution layout that can absorb later capabilities without restructuring: one folder per bounded context, shared code isolated, tests mirroring the source tree.
- A status lifecycle that is enforced server-side, so no client can produce an illegal transition.
- A walkthrough that feels instant: no page navigation, no confirmation dialogs, no spinner per tap.
- Trip lists that stay correct when the catalog changes afterwards.
- Printable output produced by the same code as the on-screen overview.

**Non-Goals:**

- Deployment topology and hosting: this change runs locally through the Aspire AppHost only.
- Any form of authentication, authorisation, or multi-tenancy.
- Background jobs, notifications, or scheduled work.
- A design system beyond the shadcn/ui defaults and a theme.

## Decisions

### D1: Two bounded contexts - `Catalog` and `Trips`

The catalog is long-lived master data edited rarely; a trip is a short-lived workflow with a high write rate on item statuses. They change for different reasons, so they get separate contexts under `src/Catalog/` and `src/Trips/`, each with `Domain`, `Application` and `Infrastructure.Sql` projects and its own `DbContext` mapped to its own SQL schema (`catalog`, `trips`) in one database.

*Alternative considered:* a single `Packing` context. Rejected because the trip lifecycle would then be forced to share an aggregate boundary and a change cadence with master data, and the packing flow is the part most likely to grow.

### D2: Trip items are snapshot copies, not references to catalog items

When categories are selected for a trip, the catalog item's name, category name, quantity and unit are copied onto the trip item. The `Trips` context stores a `SourceItemId` only as a correlation value for the "do not duplicate on re-add" rule; it holds no foreign key to catalog tables and never joins them.

Rationale: a packing list must describe what was actually packed on that trip. Renaming or deleting a catalog item months later must not rewrite history, and it keeps the two contexts independently deployable and independently testable. The cost is duplicated text and no automatic propagation of catalog edits, which is the behaviour the spec requires.

*Alternative considered:* a foreign key with `include` at query time. Rejected because it couples the contexts and makes catalog deletion destructive to past trips.

### D3: `Trip` is the aggregate root; `TripItem` is an entity inside it

All invariants that matter - a catalog item appears at most once per trip, a trip item always belongs to a selected category, status transitions are legal - live on the `Trip` aggregate. Status changes go through `trip.AdvanceItem(itemId)` / `RevertItem` / `DeleteItem` / `RestoreItem`, which load and save the aggregate.

This is a deliberate trade-off: a tap in the walkthrough loads the whole trip. Trips hold tens to a few hundred items and there is a single user, so this is cheap, and it keeps the transition rules in exactly one place. To avoid a lost update when taps overlap, `TripItem` carries a `rowversion` and a conflicting save is surfaced as a concurrency failure that the UI resolves by reverting the optimistic state.

*Alternative considered:* `TripItem` as its own aggregate root, updated with a single targeted `UPDATE`. Faster per tap, but the uniqueness and category-membership invariants would spread over two aggregates and a service. Revisit only if a trip ever grows past a few hundred items.

### D4: Deletion is a timestamp, not a status value

Persisted state keeps `Status` (`Active`, `Prepared`, `Packed`, `Loaded`) and `DeletedAt` separate, for trip items and for catalog categories and items alike. Restoring therefore needs no "previous status" bookkeeping: clearing `DeletedAt` brings the item back exactly as it was. The API and the UI project this onto the single status vocabulary the specs describe, exposing `Deleted` as a derived status that can be filtered on.

*Alternative considered:* `Deleted` as a fifth enum value plus a `PreviousStatus` column. Rejected: two columns that must be kept consistent, and every query would have to exclude one enum value by name rather than by a null check that an index can serve.

### D5: The status flow lives in one value object

`PackingStatus` is an enum, and a `PackingStatusTransition` value object owns `Next(status)` and `Previous(status)` as pure functions, implemented with a switch expression. Everything else - handlers, API, UI affordances - derives from it, including whether an advance action is offered at all. There is no second place where the order of statuses is written down, so `Loaded` having no successor cannot drift between server and client: the API returns the allowed transitions per item and the UI renders from that.

### D6: Intent-based endpoints instead of a status field

Transitions are `POST /api/trips/{tripId}/items/{itemId}:advance`, `:revert`, `:delete`, `:restore` rather than a `PATCH` that accepts a target status. The client never computes the next status, so a stale client cannot skip a step, and each endpoint maps one-to-one onto an aggregate method and a MediatR command.

*Alternative considered:* `PATCH { "status": "Packed" }`. Rejected because it invites clients to reason about the flow and turns every invalid combination into a validation error path instead of an impossible request.

### D7: Search and filtering happen client-side over a fully loaded trip list

A trip's items are fetched once by a Server Component; the search box and the filters are a Client Component that filters the already-loaded array, with matching done on `name.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()`. No request per keystroke, so the list stays responsive on a weak mobile connection - which is precisely when the feature is used.

This holds because a trip list is bounded by the catalog size. The API still exposes query parameters for category and status so that the contract does not depend on this choice; if a trip ever exceeds roughly 500 items the client can switch to the server-side path without a spec change.

For uniqueness checks in the catalog, name columns are configured with the `Latin1_General_CI_AI` collation so that "Kaarsen" and "Käarsen" collide server-side as well.

### D8: Optimistic updates with server-confirmed rollback

A tap calls a Server Action and immediately reflects the new status through `useOptimistic`. On failure the optimistic state is discarded - React restores the server state - and a Sonner toast reports it. Undo is a normal `:revert` call on that one item, not a client-side history stack, so undo works after a page refresh and cannot desynchronise from the server.

### D9: One overview component, two media

`/trips/{id}/overview` is a Server Component rendering the grouped list; printing is handled by Tailwind `print:` utilities plus `break-inside-avoid` on category blocks and the browser's own print dialog. No PDF pipeline and no separate print template, so the printed checklist cannot drift from the screen.

*Alternative considered:* server-side PDF generation. Rejected as a dependency and a second rendering path for something the browser already does.

### D10: PWA via Serwist, precaching the app shell only

`next-pwa` is unmaintained for the App Router; Serwist is its maintained successor and integrates with the Next build. The service worker precaches the shell and static assets, and uses network-only for `/api` so stale packing data can never be shown as current. The manifest is generated from `app/manifest.ts`. A new worker version prompts the user rather than silently activating mid-walkthrough.

### D11: `Result<T, Exception>` from CSharpFunctionalExtensions

The handler convention in the repository instructions needs a `Result<T, E>` type with implicit conversions from both the value and the error. `CSharpFunctionalExtensions` provides exactly that, so `Shared.Application` wires the pipeline behaviours (`TracingBehavior`, `ExceptionToResultBehavior`) around it rather than hand-rolling a result type.

### D12: Dutch UI copy, English code

All identifiers, specs, commits and comments are English; every string the user sees is Dutch, matching the product name and the user's language. UI copy is kept in the components (no i18n framework) because there is one locale.

## Risks / Trade-offs

- **No authentication at all** → The API accepts any caller. It must stay bound to localhost through the Aspire AppHost, and must not be exposed publicly or deployed until an auth capability exists. This is recorded as an explicit precondition for any future deployment change.
- **Optimistic UI can briefly show a status the server rejected** → Every action reverts on failure and raises a visible toast; the status shown after the toast is always server state, never a local guess.
- **Loading the whole `Trip` aggregate per tap** → Acceptable at the expected list sizes and a single user; `rowversion` turns an overlapping write into a clean, visible failure rather than a lost update. D3 records the escape hatch.
- **Client-side filtering does not scale** → Bounded by catalog size and covered by the existing API query parameters; the switch is a frontend change only.
- **Service worker serving stale assets after a deploy** → Versioned precache manifest plus an explicit update prompt; `/api` is never cached.
- **Testcontainers requires a running Docker daemon** → Integration tests carry the `[IntegrationTest]` trait so unit tests remain runnable without Docker, and the container image is pinned to `mcr.microsoft.com/mssql/server:2022-latest`.
- **Snapshot copies can look "wrong" to a user who just renamed a catalog item** → Intentional per D2 and visible in the spec; the trip overview shows the trip's own copy, and nothing suggests it is live.

## Migration Plan

Greenfield: no existing data and no existing consumers. The database is created by EF Core migrations per context (`catalog` and `trips` schemas), applied at startup in development through the Aspire AppHost against a SQL Server container. Rollback during development is dropping the container volume and re-running. No seed data is created.

## Open Questions

- Hosting target and the authentication approach that must precede it - out of scope here, to be decided in a separate deployment change.
- Whether a starter catalog (a ready-made set of common categories and items) is worth shipping later; it does not affect this design.
