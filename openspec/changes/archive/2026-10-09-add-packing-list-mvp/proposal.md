# Proposal

## Why

Packing for a holiday or a day trip is repetitive and error-prone: the same items are re-listed every time and it is easy to forget something in the rush before departure. There is no application yet in this repository, so we start greenfield with a reusable packing catalog plus a per-trip packing flow that works one-handed on a phone while standing at the closet or at the car.

## What Changes

- Introduce a reusable **catalog**: categories, and items that belong to exactly one category with an optional quantity and unit.
- Introduce **trips** (holiday or day trip). When a trip is created the user selects which categories to take; the app generates a trip packing list from the items in those categories.
- Give every trip item a status with a single logical forward flow: `Active -> Prepared -> Packed -> Loaded`. `Deleted` is a separate soft-delete state, reachable from any status and reversible, and excluded from the active flow.
- Provide a **walkthrough mode** optimised for advancing many items quickly: one tap per item moves it to the next logical status, with undo.
- Provide **search** across all active items of a trip regardless of category, with filters on category and status.
- Provide a **full-screen overview** of all trip items, grouped by category, with a print-friendly layout.
- Deliver the frontend as a **mobile-first installable PWA** (installable app shell, responsive touch targets, works on any modern mobile browser).
- Establish the initial solution skeleton: .NET backend (DDD, MediatR vertical slices, EF Core/SQL Server, Aspire AppHost + ServiceDefaults) and a Next.js App Router frontend with shadcn/ui and Tailwind v4, including the test projects required by the repository conventions.

Assumptions recorded (the user did not specify): single user, **no authentication** in this MVP; the PWA is installable and the app shell works offline, but **data operations require network** - offline data sync is explicitly out of scope.

Non-goals: multi-user accounts and sharing, offline write sync, templates/import-export, photo attachments, weather- or destination-based suggestions.

## Capabilities

### New Capabilities

- `packing-catalog`: Managing reusable categories and catalog items, including the optional quantity/unit on an item and soft-deletion of catalog entries.
- `trip-packing`: Creating a trip, selecting the categories to take, generating the trip packing list, and the item status lifecycle (`Active -> Prepared -> Packed -> Loaded`, plus `Deleted`) including the fast walkthrough interaction and trip progress.
- `packing-search`: Searching active trip items across all categories and filtering the trip list by category and status.
- `packing-overview`: The full-screen, category-grouped overview of a trip's items and its print-friendly rendering.
- `pwa-shell`: Installability, mobile-first layout requirements, and app-shell availability of the web client.

### Modified Capabilities

<!-- None. This is the first change in a greenfield repository; there are no existing specs. -->

## Impact

- **New code**: `src/Catalog/*`, `src/Trips/*` (Domain / Application / Infrastructure.Sql per the bounded-context convention), `src/Shared/*`, `src/Web` (API), `src/AppHost`, `src/ServiceDefaults`, and a Next.js client under `src/web-client`.
- **New tests**: `tests/<Context>/<Context>.<Layer>.Tests` with xUnit, Moq for application-layer handlers and Testcontainers (`mcr.microsoft.com/mssql/server:2022-latest`) for repository integration tests, plus `WebApplicationFactory` HTTP tests.
- **Dependencies**: .NET 10 / C# 14, EF Core + SQL Server, MediatR, .NET Aspire, OpenTelemetry; Next.js 15 App Router, React 19, Tailwind CSS v4, shadcn/ui, lucide-react, react-hook-form + zod, Sonner. Central Package Management via `Directory.Packages.props`.
- **Infrastructure**: local orchestration through the Aspire AppHost with a SQL Server container; database schema created via EF Core migrations.
- **Data**: no existing data to migrate; the catalog starts empty (no seed data).
