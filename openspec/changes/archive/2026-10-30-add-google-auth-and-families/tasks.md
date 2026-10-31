# Tasks

## 1. Setup

- [x] 1.1 Write `docs/google-sign-in.md` with the manual steps: create a Google Cloud project and OAuth client (type Web application), authorised redirect URI `http://localhost:3000/api/auth/callback/google`, add family members as test users while the consent screen is in testing mode, and fill the Aspire parameters `google-client-id` and `google-client-secret`. Verify by following the document once and confirming the client id and secret are stored in the AppHost user secrets.
- [x] 1.2 Add `Microsoft.AspNetCore.Authentication.JwtBearer` to `Directory.Packages.props` and `Web.csproj`, and `next-auth@4` and `jose` to the web-client. Verify `dotnet build` and `npm run build` succeed.
- [x] 1.3 Wire the AppHost (design D10): fixed frontend port 3000, `NEXTAUTH_URL`, parameters `google-client-id`, `google-client-secret`, generated and persisted `nextauth-secret` and `api-token-key`, and pass them to the frontend and API. Verify with `aspire run` that the dashboard shows the frontend on `http://localhost:3000` and both resources receive the environment variables.

## 2. Families context

- [x] 2.1 Create `Families.Domain` with `Family`, `FamilyName`, `Member`, `EmailAddress` (lower-cased, validated) and `KnownFamilies.Verburgh`. Verify with tests in `tests/Families/Families.Domain.Tests` covering name rules, e-mail normalisation and validation, adding a duplicate member, and removing a member.
- [x] 2.2 Create `Families.Infrastructure.Sql` with `FamiliesDbContext` (schema `families`), configurations with unique indexes on family name and member e-mail, the repository, and the initial migration that inserts "Verburgh" with member `ivoverburgh@gmail.com`. Verify with `[IntegrationTest]` tests in `tests/Families/Families.Infrastructure.Sql.Tests`: the migration creates Verburgh with its member, a family round-trips, and a second family with an existing member's e-mail is rejected by the database.
- [x] 2.3 Create `Families.Application` with handlers `CreateFamily`, `RenameFamily`, `ListFamilies`, `AddMember` (rejects an e-mail already in a family and names that family), `RemoveMember` and `GetMembershipByEmail`. Verify with Moq-based handler tests in `tests/Families/Families.Application.Tests` for success, not-found and validation paths.

## 3. API authentication and current user

- [x] 3.1 Add `ForbiddenException` to `Shared.Validation` and map it to 403 in `ResultExtensions`. Verify with a unit test of the mapping.
- [x] 3.2 Configure JWT bearer validation (issuer, audience, HS256 key from `Auth:TokenKey`, lifetime) and require authentication on every endpoint except health checks; add `Access:Administrators` with `ivoverburgh@gmail.com` to `appsettings.json`. Verify with a Web test class using real JWT validation: no token → 401, expired token → 401, token signed with another key → 401, `/health` without token → 200.
- [x] 3.3 Add the `CurrentUser` service and middleware, the `RequireMember` and `RequireAdministrator` endpoint filters, and `GET /api/me` (design D3). Verify with Web tests: administrator without family, member, and unknown account (403) on `/api/me`; e-mail compared case-insensitively.
- [x] 3.4 Add `TestAuthenticationHandler` and rework `ApiFixture` to create two families with one member each and offer `ClientFor(email)`; switch every existing Web test to an authenticated member client. Verify the full Web test suite passes.
- [x] 3.5 Add administrator endpoints for families and members (`/api/admin/families`, `/api/admin/families/{id}/members`) behind `RequireAdministrator`. Verify with Web tests: the administrator creates, renames and lists families and adds and removes members; a member gets 403; adding a member of another family returns 400 naming that family; a removed member gets 403 on `/api/me` at the next request.

## 4. Catalog scoping

- [x] 4.1 Add `FamilyId` to `Category`, `CatalogActor`, `Category.AccessFor(actor)` and the same-scope rule in `CatalogItem.MoveTo` (design D5). Verify with domain tests for every access combination and for a rejected cross-scope move.
- [x] 4.2 Update the EF configuration (scoped unique index, opt-out table `FamilyCategoryOptOuts`) and add the migration that moves non-seed categories to Verburgh and leaves the 22 seed categories general (design D7). Verify with `[IntegrationTest]` tests: the same name in two families and in general is accepted, a duplicate within one scope is rejected, and the migration applied to a database with seeded and non-seeded categories gives the expected `FamilyId` values.
- [x] 4.3 Make every catalog handler take a `CatalogActor` and enforce access: not found for another family's data, forbidden for member writes to general data; listing returns own plus switched-on general categories (optionally the switched-off ones); add `SwitchGeneralCategory` and `ListGeneralCategories`. Verify with Moq-based handler tests for success, not-found and forbidden paths per handler.
- [x] 4.4 Update the catalog endpoints: pass the actor from `CurrentUser`, `general` flag on create (administrator only), `includeSwitchedOff` on list, `PUT /api/categories/{id}/switch`, `GET /api/categories/general`. Verify with Web tests covering the catalog scenarios of the `packing-catalog` delta spec, including two families not seeing each other's categories.

## 5. Trips scoping

- [x] 5.1 Add `FamilyId` to `Trip` (required in `Trip.Create`), filter `ITripRepository` on it, and add the migration assigning existing trips to Verburgh. Verify with domain tests, repository integration tests (another family's trip is not found, lists are filtered) and a migration test on an existing trip.
- [x] 5.2 Pass the caller's family to every trip handler and build category selections from the family's available categories only. Verify with handler tests and Web tests covering the `trip-packing` delta scenarios: mixing own and general categories, ignoring a switched-off or foreign category, 404 for another family's trip, 403 for an administrator without a family creating a trip.

## 6. Development seed

- [x] 6.1 Make `CatalogSeeder` seed general categories and check name clashes only against general ones; make `DevelopmentSeeder` create both trips for `KnownFamilies.Verburgh`. Verify by updating `CatalogSeederTests`, `TripsSeederTests` and the Development Web tests: seeded categories are general, both trips belong to Verburgh and are invisible to another family, and a family category named `Keukengerei` does not block the seed.

## 7. Frontend authentication

- [x] 7.1 Add `src/auth.ts` and the route handler `app/api/auth/[...nextauth]/route.ts` with the Google provider, JWT sessions, and a `signIn` callback that refuses unverified e-mail and asks `GET /api/me`. Verify manually: signing in as the administrator succeeds; an unknown Google account lands on `/geen-toegang` showing its e-mail.
- [x] 7.2 Add `src/proxy.ts` (optimistic redirect to `/inloggen?next=…`) and the DAL `lib/session.ts`; make `lib/api.ts` mint the API token per call and redirect on 401/403. Verify manually: opening `/catalogus` signed out goes to `/inloggen` and back to `/catalogus` after sign-in; removing yourself as a test member in `/beheer` from another session sends the next page load to `/geen-toegang`.
- [x] 7.3 Build `/inloggen`, `/geen-toegang`, the account menu with "Uitloggen", and the "no family" explanation for an administrator without a family. Verify manually on a phone-sized viewport and with `npm run lint` and `npm run build` passing.

## 8. Frontend catalog and administration

- [x] 8.1 Show general categories on `/catalogus` in their own section with an "Algemeen" badge, read-only, with an on/off switch, and keep own categories editable. Verify manually: switching "Kampeerkeuken" off removes it from the trip category picker; switching it on brings it back.
- [x] 8.2 Build `/beheer` (families with members, create and rename a family, add and remove members) and `/beheer/catalogus` (general catalog editing reusing the catalog components), and add the "Beheer" tab for administrators only. Verify manually as administrator and as a non-administrator member (no tab, `/beheer` shows no access).

## 9. End-to-end check

- [x] 9.1 Run `dotnet build`, `dotnet test`, `npm run lint` and `npm run build`, and verify all succeed without warnings.
- [x] 9.2 With `aspire run`, verify the migration on the existing local database: the 22 seeded categories are general, the other existing categories and all existing trips belong to "Verburgh", and `ivoverburgh@gmail.com` sees them after signing in.
- [x] 9.3 Verify family isolation end to end with a second Google test account: create family "Test" in `/beheer`, add the account, sign in with it in a private window, and confirm it sees the general catalog but none of Verburgh's own categories or trips; then remove it and confirm the next page load shows `/geen-toegang`.

## Workflow follow-up

- Archive with `/opsx:archive` after review; per project preference, archiving is followed by commit and push.
