# Proposal

## Why

PakEchtMee has no authentication: anyone who can reach the frontend or the API port can read and change every packing list. The app is about to be shared with other families, who each need their own catalog and trips, while a common base list should be maintained once instead of being copied by every family. This is the moment to introduce identities, families and a shared catalog, before more data piles up in a single unowned pool.

## What Changes

- **BREAKING** Every page and every API endpoint (except health checks) requires a signed-in Google account. Unknown accounts are refused with a "geen toegang" page.
- Sign-in with Google through the Next.js frontend. `ivoverburgh@gmail.com` is configured as administrator and can always sign in, also without belonging to a family.
- New **families**: the administrator creates and renames families and adds or removes Google accounts (by e-mail address) as members. An account belongs to at most one family. Only the administrator manages families and members.
- **BREAKING** Categories, catalog items and trips become private to a family. Members only ever see and change their own family's data; data of another family is indistinguishable from data that does not exist.
- New **general catalog**: categories and items that belong to no family, maintained only by the administrator. Every family sees the general categories it has switched on, read-only, next to its own categories, and can use both when generating a trip. A family can switch general categories off and on again at any time; changes the administrator makes show up immediately.
- Families keep creating and editing their own categories and items. Own items only live in own categories, never inside a general category.
- Existing data is migrated: a family "Verburgh" is created with `ivoverburgh@gmail.com` as member; the seeded Excel catalog becomes the general catalog; all other existing categories and all existing trips move to "Verburgh".
- The development seed puts its catalog in the general catalog and its two trips in "Verburgh".
- The frontend gets a sign-in page, a sign-out action, and administrator pages for families, members and the general catalog.
- Local development uses real Google sign-in. This requires a one-time Google OAuth client (created by the user in Google Cloud Console) and a fixed frontend port so the redirect URI is stable.

## Capabilities

### New Capabilities

- `access-control`: Who can use the app at all - Google sign-in, the always-on administrator, refusing unknown accounts, sign-out, and the guarantee that the API serves nothing without a verified identity.
- `family-management`: Families and their members - creating and renaming families, adding and removing member accounts by e-mail, one family per account, and that only the administrator can do this.

### Modified Capabilities

- `packing-catalog`: Categories and items are scoped to a family or to the general catalog; name uniqueness applies per scope; only the administrator edits general categories and items; families switch general categories on or off.
- `trip-packing`: Trips belong to a family and are only visible to its members; a trip can be generated from the family's own categories and its switched-on general categories.
- `development-seed-data`: The seeded catalog becomes the general catalog and the seeded trips belong to the family "Verburgh"; name clashes are only checked against the general catalog.

## Impact

- **New bounded context** `Families` (`src/Families/`): domain, application and SQL infrastructure in schema `families`, with migrations that create the "Verburgh" family and its first member.
- **Catalog context**: `Category` gets an optional family owner; a per-family list of switched-off general categories; handlers take the acting family and role; new migration including the data move.
- **Trips context**: `Trip` gets a family owner; handlers filter on it; new migration assigning existing trips to "Verburgh".
- **Web (API)**: JWT bearer authentication for tokens minted by the frontend; a request-scoped current-user that resolves the family through the Families context; administrator configuration; new endpoints for the current user, families, members and the general catalog; authorization on every existing endpoint.
- **web-client**: `next-auth` (v4) with the Google provider, `proxy.ts` for optimistic redirects, a data access layer that verifies the session and attaches a short-lived API token to every API call, new sign-in, no-access and administrator pages, navigation changes.
- **AppHost**: fixed frontend port 3000; parameters for the Google client id and secret (supplied by the user) and generated secrets for the session and the frontend-to-API token.
- **Tests**: Web integration tests switch to a test authentication handler; every existing endpoint test needs an authenticated member; new unit and integration tests for families, scoping and authorization.
- **Dependencies**: `next-auth` and `jose` in the web-client; `Microsoft.AspNetCore.Authentication.JwtBearer` in Web.
