# Design

## Context

See proposal.md - Why. Requirements are in the five delta specs under `specs/`.

Current state that shapes the approach:

- The browser never calls the API. Every API call is made server-side by Next.js (`src/web-client/src/lib/api.ts` is `server-only`; mutations go through Server Actions in `lib/actions.ts`). Next.js is therefore a backend-for-frontend, and the API only has to trust one caller.
- The API (`src/Web`) has no authentication and listens on a fixed, unproxied port 5080 (AppHost comment: so a separately started Next dev server can reach it). On a developer machine that port is reachable by anything local, so the API must enforce access itself.
- Next.js is version 16: `middleware.ts` is renamed `proxy.ts`, and the bundled guide (`node_modules/next/dist/docs/01-app/02-guides/authentication.md`) recommends optimistic checks in Proxy plus a Data Access Layer (`verifySession()`) close to the data.
- Catalog and Trips are separate bounded contexts with their own schema; only `Web` sees both (archived MVP design D1). Commands are MediatR vertical slices returning `Result<T, Exception>`; `ResultExtensions` maps exception types to HTTP status codes.
- The project instructions already anticipate a `TestAuthenticationHandler` behind a custom `WebApplicationFactory` for HTTP tests.
- The frontend port is assigned by Aspire at random; Google OAuth requires an exact, pre-registered redirect URI.

## Goals / Non-Goals

**Goals:**

- No data leaves the API without a verified identity, also when the API port is called directly.
- One place in the API decides who the caller is and which family they act for; handlers receive that decision as data, so the Catalog and Trips contexts never learn about Google or families' internals.
- The administrator can never be locked out by data: administrator status comes from configuration, not from the database.
- Removing a member takes effect on the next request, without waiting for a session to expire.

**Non-Goals:**

- Production hosting, HTTPS certificates, and a published Google OAuth consent screen. This change runs locally through Aspire like everything before it.
- Deleting families, transferring data between families, or an account in several families.
- Invitations by e-mail. The administrator adds an address and tells the person out of band.
- Roles below administrator (for example family managers) and per-trip sharing.
- Automated frontend tests; the web-client has no test setup today. Frontend behaviour is verified end-to-end in the last task group.

## Decisions

### D1: Google sign-in lives in Next.js with `next-auth` v4

`next-auth` 4.24 (stable, peer range includes Next 16) runs the Google OAuth flow in a route handler at `/api/auth/[...nextauth]` on the Next.js server, with the JWT session strategy (an encrypted cookie, no session table). The `signIn` callback refuses accounts whose `email_verified` is false and then asks the API whether the address is allowed (D3); a refused sign-in redirects to `/geen-toegang`, which reads the refused address from a short-lived, HTTP-only cookie so it never appears in a URL or browser history.

*Alternatives considered:*
- `next-auth` v5: still `5.0.0-beta` at the time of writing.
- Better Auth: stable and Next 16 compatible, but built around its own database tables for users and sessions, which duplicates what the Families context owns.
- ASP.NET Core handling the OpenID Connect flow: the browser does not talk to the API, so the cookie would live on the wrong origin and every page request would need proxying.

### D2: Next.js calls the API with a short-lived token it signs itself

For every API call, the Data Access Layer mints a JWT with `jose`: HS256, subject = the lower-cased e-mail address, issuer `pakechtmee-web`, audience `pakechtmee-api`, lifetime 5 minutes. The API validates it with `JwtBearer` against the same symmetric key (Aspire parameter `api-token-key`, generated and persisted). The Google tokens never leave the Next.js server.

*Alternatives considered:*
- Forwarding the Google ID token: it expires after an hour and needs refresh handling, and the API would then depend on Google's signing keys and clock for every request.
- Relying on network isolation of the API port: not true on a developer machine, and the spec requires 401 for direct calls.

### D3: One access decision per request in the API

A `CurrentUser` service (scoped) is filled by a small middleware after authentication: e-mail from the token, `IsAdministrator` from configuration `Access:Administrators` (contains `ivoverburgh@gmail.com` in `appsettings.json`), and `FamilyId` from the Families context (`GetMembershipByEmail` query). Endpoint groups declare what they need through two endpoint filters:

- `RequireMember` - 403 unless `FamilyId` is set; used by trips and the family catalog.
- `RequireAdministrator` - 403 unless `IsAdministrator`; used by families, members and general-catalog writes.

`GET /api/me` returns `{ email, isAdministrator, family: { id, name } | null }` and answers 403 for accounts that are neither; the `signIn` callback (D1) and the layout use it. Because the membership is looked up on every request, removing a member is effective immediately (spec: "Losing access takes effect immediately"); the cost is one indexed lookup per request.

*Alternative considered:* putting family id and role into the session at sign-in. Cheaper per request, but a removed member would keep access until the session expires.

### D4: A `Families` bounded context

`src/Families/` with `Families.Domain`, `Families.Application`, `Families.Infrastructure.Sql`, schema `families`. `Family` is the aggregate root with `FamilyName` (max 60, unique case-insensitively) and a list of `Member` entities holding a lower-cased `EmailAddress` value object. A unique index on `Members.Email` enforces "one family per account" in the database as well as in the handler, which gives the friendly error naming the existing family.

Catalog and Trips do not reference this context. They store a `FamilyId` as a plain value, the same way Trips stores `SourceItemId` (archived MVP design D2).

*Alternative considered:* adding families to the Catalog context. Rejected: membership changes for different reasons than packing data, and Trips would then have to depend on Catalog for access checks.

### D5: Catalog scope is a nullable family id on the category

`Category.FamilyId` is `Guid?`; `null` means general. Items inherit the scope of their category and get no column of their own. The filtered unique index changes from `(Name)` to `(FamilyId, Name)` where `DeletedAt IS NULL`; SQL Server treats NULLs as equal in a unique index, so general names stay unique among themselves. `MoveTo` is rejected when source and target scope differ.

Switching general categories off is stored as an opt-out: table `catalog.FamilyCategoryOptOuts (FamilyId, CategoryId)`. A general category is on unless a row exists, which makes new general categories appear for every family without touching any family (spec: "New general category appears everywhere").

Handlers receive a `CatalogActor(Guid? FamilyId, bool IsAdministrator)` from the endpoint. The rule is decided in one domain method, `Category.AccessFor(actor)` → `Edit`, `Read` or `None`:

- own category of the actor's family → `Edit`;
- own category of another family → `None` (handler answers `NotFoundException`);
- general category → `Edit` for administrators, `Read` for members (a write answers the new `ForbiddenException`, mapped to 403 in `ResultExtensions`).

API shape: the existing category and item endpoints keep their routes. `POST /api/categories` takes `general: bool` (only allowed for administrators). `GET /api/categories` returns the family's own plus switched-on general categories with an `isGeneral` flag; `?includeSwitchedOff=true` adds the switched-off general ones with `isSwitchedOn: false` for the toggle screen. `PUT /api/categories/{id}/switch` with `{ on: bool }` toggles a general category for the caller's family. `GET /api/categories/general` lists the general catalog for the administrator.

*Alternatives considered:*
- Copying general categories into each family: rejected by the user in favour of live categories.
- An opt-in table: every new general category would have to be switched on by every family first.

### D6: Trips carry a family id and are only found within it

`Trip.FamilyId` is required and set by `Trip.Create`. `ITripRepository.FindAsync(id, familyId)` and `ListAsync(familyId)` filter on it, so a trip of another family is simply not found - every existing handler keeps its "not found" path and needs no new branch. The category snapshots for a trip are built by `CategorySelections` (Web) from the catalog query for the caller's family, which only returns categories available to it; that is what makes a switched-off or foreign category produce no items.

### D7: Data migration with a well-known family id

The family "Verburgh" gets the fixed id `0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00`, exposed as `KnownFamilies.Verburgh` in `Families.Domain` and repeated as a literal in the Catalog and Trips migrations (migrations are frozen snapshots and must not reference live code). One migration per context:

- `families`: create tables, insert "Verburgh" and member `ivoverburgh@gmail.com`.
- `catalog`: add `FamilyId`, the opt-out table and the new index; set `FamilyId` to Verburgh for every existing category whose id is not one of the 22 seed category ids (listed as literals in the migration). Seeded categories stay `NULL` and become general.
- `trips`: add `FamilyId` with Verburgh as default for existing rows, then drop the default.

Migrations run at startup in Development as today.

### D8: Development seed targets the general catalog and Verburgh

`CatalogSeeder` creates general categories (`FamilyId = null`) and only checks name clashes against non-removed general categories. `DevelopmentSeeder` passes `KnownFamilies.Verburgh` to `TripsSeeder` and builds the trip snapshots with a Verburgh member actor. No family is seeded: Verburgh comes from the migration.

### D9: Frontend structure

- `src/auth.ts`: `next-auth` options (Google provider, callbacks), shared by the route handler and the server code.
- `src/proxy.ts`: optimistic check only - no session cookie → redirect to `/inloggen?next=…`; it never calls the API (guide: Proxy runs on prefetches too).
- `src/lib/session.ts` (DAL): `verifySession()` memoised with React `cache`, returning the e-mail or redirecting to `/inloggen`. `api.ts` calls it and mints the API token (D2); an API 401 redirects to `/inloggen`, a 403 to `/geen-toegang`.
- New pages: `/inloggen`, `/geen-toegang`, `/beheer` (families with members, add/remove), `/beheer/catalogus` (general catalog, reusing the catalog components with a `general` flag).
- `/catalogus` shows own categories and, in a separate section, general categories with a "Algemeen" badge, read-only, each with an on/off switch.
- Bottom navigation gets a "Beheer" tab for administrators; a header menu holds the account e-mail and "Uitloggen". An administrator without a family sees a short explanation on `/` and `/catalogus` with a link to `/beheer`.
- The service worker keeps `/api/*` and navigations network-only (unchanged), so no page of one account is ever served from cache to another.
- `proxy.ts` marks the public pages (`/inloggen`, `/geen-toegang`, `/offline`) with a request header so the layout renders no account details there; the offline page is precached by the service worker and must not contain an e-mail address.

### D10: Aspire wiring

- Frontend endpoint fixed at port 3000 (`WithHttpEndpoint(port: 3000, env: "PORT")`) and `NEXTAUTH_URL=http://localhost:3000`, so the Google redirect URI `http://localhost:3000/api/auth/callback/google` is stable.
- Parameters: `google-client-id` and `google-client-secret` (secret), supplied by the user; `nextauth-secret` and `api-token-key` generated once and persisted in user secrets. `api-token-key` goes to both the API (`Auth__TokenKey`) and the frontend (`API_TOKEN_KEY`).
- The CORS policy stays as is; the browser still never calls the API.

### D11: Tests

- Web integration tests replace JWT bearer with a `TestAuthenticationHandler` (scheme "Test") that reads the e-mail from an `X-Test-User` header. `ApiFixture` creates two families with a member each and an administrator setting, and offers `ClientFor(email)`. Every existing endpoint test uses a member client.
- A separate test class keeps the real JWT bearer setup to prove 401 for a missing, expired or wrongly signed token.
- Domain tests for `Family`, `EmailAddress`, `Category.AccessFor` and the move rule; handler tests with Moq for the success, not-found and forbidden paths; Testcontainers tests for the scoped unique index, the opt-out table, family-filtered trip queries and the three migrations on existing data.

## Risks / Trade-offs

- [The Google OAuth client is a manual step outside the repository] → Tasks start with written instructions; until the two parameters are filled in, the Aspire dashboard shows them as missing and the frontend shows a clear configuration error on `/inloggen`.
- [A Google OAuth app in "testing" mode only admits listed test users] → Documented in the setup instructions: add every family member as a test user, or publish the consent screen later.
- [Port 3000 can be taken on the developer machine] → Aspire reports the conflict at startup; changing the port means updating the redirect URI in Google too, which the instructions mention.
- [One membership lookup per API request] → An indexed lookup on a small table; acceptable for a family app. Revisit with a short in-memory cache if it ever shows up in traces.
- [The shared HS256 key lets anyone holding it impersonate any account against the API] → The key only exists in user secrets and in the two processes' environments, never in the browser or the repository.
- [Existing Web tests all change] → Done in the same task group as the authentication itself, so the suite is green at every group boundary.

## Migration Plan

1. Create the Google OAuth client and fill the Aspire parameters (manual, see tasks).
2. Start the AppHost: the three migrations run in Development, move existing data to "Verburgh" and make the seeded catalog general.
3. Sign in as `ivoverburgh@gmail.com`; add other members in `/beheer`.

Rollback: revert the code and run `dotnet ef database update` to the previous migration per context, or drop the local SQL volume and let the seed rebuild the data.
