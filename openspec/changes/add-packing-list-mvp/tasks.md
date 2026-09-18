# Tasks

## 1. Solution foundation

- [x] 1.1 Create `PakEchtMee.sln`, `Directory.Build.props` (net10.0, C# 14, nullable enabled, warnings as errors), `Directory.Packages.props` for Central Package Management, and `.editorconfig`; verify `dotnet build` succeeds on the empty solution
- [x] 1.2 Add `src/Shared/Shared.Application` with the MediatR registration extension, `TracingBehavior<TRequest, TResponse>` and `ExceptionToResultBehavior<TRequest, TResponse>` (rethrowing `OperationCanceledException`, passing through non-`Result` responses) per design D11; verify unit tests in `tests/Shared/Shared.Application.Tests` cover exception-to-failure conversion, cancellation rethrow and pass-through
- [x] 1.3 Add `src/Shared/Shared.Validation` with the shared guard helpers used by the domain value objects; verify its unit tests pass
- [x] 1.4 Add `tests/Shared/Shared.Tests` with the `[IntegrationTest]` trait attribute and the `SqlServerTestContainerFixture` (`mcr.microsoft.com/mssql/server:2022-latest`); verify a smoke integration test can open a connection to the container
- [x] 1.5 Add `src/ServiceDefaults` (OpenTelemetry tracing/metrics/logging, health checks) and `src/AppHost` orchestrating a SQL Server container and the Web project; verify `aspire run` starts the dashboard with both resources healthy

## 2. Catalog domain

- [x] 2.1 Implement `CategoryName` and `ItemName` value objects with the length and non-empty rules from `specs/packing-catalog/spec.md`; verify unit tests cover empty, whitespace-only, max length and over-length input
- [x] 2.2 Implement the `Quantity`/`Unit` value object enforcing quantity 1..9999 and rejecting a unit without a quantity; verify unit tests cover quantity 0, negative, 10000, unit-without-quantity and the valid combinations
- [x] 2.3 Implement the `Category` aggregate with rename, soft delete via `DeletedAt` and restore (design D4), and `CatalogItem` with rename, move-to-category and soft delete, rejecting creation in a removed category; verify unit tests cover every scenario in `specs/packing-catalog/spec.md`

## 3. Catalog application

- [x] 3.1 Define the `ICategoryRepository` and `ICatalogItemRepository` interfaces in the domain and the `Catalog.Application` project with `AddMediatR` assembly scanning and the shared pipeline behaviours registered as open generics; verify the DI registration test resolves the pipeline
- [x] 3.2 Implement the category handlers (create, rename, remove, restore, list) as vertical slices with nested command/query records returning `Result<T, Exception>`, including the case-insensitive uniqueness check among non-removed categories; verify Moq-based unit tests cover the success path, the duplicate-name rejection and the repository-failure path per handler
- [x] 3.3 Implement the catalog item handlers (create, rename, move, remove, restore, list by category) including uniqueness within a category and rejection for a removed category; verify Moq-based unit tests cover success, validation rejection and repository failure per handler

## 4. Catalog infrastructure

- [x] 4.1 Add `Catalog.Infrastructure.Sql` with `CatalogDbContext` mapped to the `catalog` schema, `Latin1_General_CI_AI` collation on the name columns (design D7), filtered indexes on `DeletedAt IS NULL`, and the initial EF Core migration; verify `dotnet ef migrations script` generates the expected schema
- [x] 4.2 Implement the repositories without try/catch and register the context and repositories as scoped in a `ServiceCollectionExtensions`; verify Testcontainers integration tests cover CRUD, soft delete/restore, accent-insensitive duplicate detection and a query for a non-existing id

## 5. Trips domain

- [x] 5.1 Implement `PackingStatus` and the `PackingStatusTransition` value object with `Next`/`Previous` switch expressions as the single source of the flow (design D5); verify unit tests assert every forward and backward step and that `Loaded` has no next and `Active` no previous
- [x] 5.2 Implement `TripName` and the trip date rule (end not before start); verify unit tests cover missing dates, equal dates and end-before-start
- [x] 5.3 Implement the `Trip` aggregate root with `TripItem` entities carrying the snapshot fields and `SourceItemId` (design D2/D3), plus `SelectCategories`, `RemoveCategory`, `AdvanceItem`, `RevertItem`, `DeleteItem`, `RestoreItem` and the status/progress counts excluding deleted items; verify unit tests cover every scenario in `specs/trip-packing/spec.md`, including no duplicates on re-adding a category and rejection of a non-single-step transition

## 6. Trips application

- [x] 6.1 Create `Trips.Application` with its MediatR registration and the `ITripRepository` interface; verify the DI registration test resolves the handlers and the pipeline
- [x] 6.2 Implement the trip handlers (create trip, list trips ordered with upcoming/ongoing first, get trip with items, select categories, remove category) where selecting categories receives already-read catalog item snapshots rather than querying the catalog context; verify Moq-based unit tests cover success, validation rejection and repository failure per handler
- [x] 6.3 Implement the item status handlers (advance, revert, delete, restore) returning the updated item with its allowed transitions; verify Moq-based unit tests cover each legal transition, each rejected transition and the repository-failure path

## 7. Trips infrastructure

- [x] 7.1 Add `Trips.Infrastructure.Sql` with `TripsDbContext` mapped to the `trips` schema, `TripItem` owned by the `Trip` aggregate, a `rowversion` concurrency token on `TripItem` (design D3), indexes for trip-scoped queries, and the initial migration; verify the generated script contains no foreign key to the `catalog` schema
- [x] 7.2 Implement `TripRepository` loading the aggregate with its items and register the context and repository as scoped; verify Testcontainers integration tests cover generating a list from categories, status transitions, soft delete/restore, a concurrent update surfacing as a concurrency failure, and loading a non-existing trip

## 8. Web API

- [x] 8.1 Create `src/Web` referencing `ServiceDefaults`, both application and infrastructure contexts, with migrations applied on startup in development; verify the health endpoint reports healthy when started from the AppHost
- [x] 8.2 Add the catalog minimal API endpoints for categories and items with problem-details responses for validation failures; verify `WebApplicationFactory` tests assert 200/201 on success and 400 with a problem-details body on each validation rejection
- [x] 8.3 Add the trip endpoints including the intent-based transitions `:advance`, `:revert`, `:delete`, `:restore` (design D6), the trip item query with `category` and `status` query parameters, and `Deleted` projected as a derived status (design D4); verify `WebApplicationFactory` tests assert the flow end to end and that an illegal transition returns 400 without changing state
- [x] 8.4 Configure CORS for the local frontend origin only and bind the API through the AppHost without exposing it publicly (design risk: no authentication); verify a request from an unlisted origin is rejected

## 9. Frontend foundation and PWA

- [x] 9.1 Scaffold the Next.js App Router client in `src/web-client` with TypeScript strict mode, Tailwind CSS v4, shadcn/ui, lucide-react and Sonner, and a typed API client reading the Aspire-provided base URL; verify `npm run build` and `npm run lint` succeed
- [x] 9.2 Implement the mobile-first app shell: bottom navigation reachable one-handed, 44x44 CSS pixel minimum touch targets, no horizontal scrolling from 320 pixels, and `prefers-reduced-motion` honoured; verify the scenarios in `specs/pwa-shell/spec.md` at 320 and 768 pixel viewports
- [ ] 9.3 Add `app/manifest.ts` with name, short name, start URL, `standalone` display, theme and background colours, and maskable 192 and 512 pixel icons; verify the browser offers installation and the installed app opens without address bar chrome
- [ ] 9.4 Add Serwist with app-shell precaching, network-only for `/api`, and an update prompt instead of silent activation (design D10); verify launching offline renders the shell with an offline state and a retry action, and that retrying after reconnecting loads the data
- [ ] 9.5 Add the global failure handling for mutations: revert to server state and show a Dutch error toast, never presenting an unsaved change as saved; verify an offline status change visibly reverts and toasts

## 10. Catalog screens

- [ ] 10.1 Build the category screen (list, create, rename, remove, restore) with `react-hook-form` plus `zod` validation mirroring the server rules; verify the duplicate-name and empty-name rejections surface as inline field errors
- [ ] 10.2 Build the item screen per category (create, rename, move, remove, restore) with optional quantity and unit, rendering "Socks 7 pair", "Towel 2" and "Passport" as specified; verify the display and validation scenarios in `specs/packing-catalog/spec.md`

## 11. Trip and walkthrough screens

- [ ] 11.1 Build the trip list and the create-trip form with optional dates and the end-not-before-start rule; verify upcoming and ongoing trips sort first and an invalid date range is rejected inline
- [ ] 11.2 Build the category selection for a trip, generating the list and appending only missing items on re-add, with a confirmation dialog when removing a category whose items have progressed; verify the generation scenarios in `specs/trip-packing/spec.md`
- [ ] 11.3 Build the walkthrough list where one tap advances an item through a Server Action with `useOptimistic`, no confirmation and no navigation, hiding the advance action on `Loaded` items and rendering affordances from the server-supplied allowed transitions; verify a tap updates in place, scroll position is preserved and a failed change reverts with a toast
- [x] 11.4 Add the per-item undo calling `:revert` on that single item, and the delete/restore actions (design D8); verify undo after an advance affects only that item and survives a page refresh
- [x] 11.5 Show the trip progress counts per status and the loaded share, excluding deleted items and updating immediately after each change; verify the progress scenarios in `specs/trip-packing/spec.md`

## 12. Search and filters

- [x] 12.1 Implement the client-side search over the loaded trip items with NFD diacritic stripping, case-insensitive substring matching and results updating while typing (design D7); verify the matching scenarios in `specs/packing-search/spec.md`, including that deleted items are excluded by default
- [x] 12.2 Implement the category and status filters as union within a kind and intersection across kinds, with visible active filters and a single clear-all action; verify the filter scenarios and the combined query-plus-filter scenario
- [x] 12.3 Add the empty state naming the active query and filters with an action to clear them; verify it appears only when the combination yields no results

## 13. Overview and print

- [x] 13.1 Build the full-screen overview at `/trips/[id]/overview` grouped by category showing name, amount, status and the per-status counts, excluding deleted items, with an empty state for a trip without items; verify the overview scenarios in `specs/packing-overview/spec.md`
- [x] 13.2 Apply the active filters to the overview and state on the page which filters are applied; verify a filtered overview shows only matching items and names the filter
- [ ] 13.3 Add the print layout with `print:` utilities, a checkbox per item, `break-inside-avoid` on category blocks, no interactive chrome, A4 portrait width and status legible without colour (design D9); verify the print preview scenarios, including a multi-page list

## 14. End-to-end verification

- [ ] 14.1 Run `dotnet test` for the full solution including the Testcontainers integration tests and confirm every test passes with Docker running
- [ ] 14.2 Walk the primary journey against the AppHost on a 390 pixel viewport - create categories and items, create a trip, select categories, advance all items to `Loaded`, undo one, search, filter, open the overview and print - and confirm each spec scenario behaves as written
- [ ] 14.3 Run a Lighthouse PWA and mobile audit on the built client and confirm the installability and app-shell requirements in `specs/pwa-shell/spec.md` are met
