# Design

## Context

Today only Aspire runs the app locally (`src/AppHost/AppHost.cs`):

- **Database.** A SQL Server container with a password parameter.
- **API.** The `Web` project on port 5080.
- **Frontend.** The Next.js app started with `npm run dev` through `AddJavaScriptApp`.
- **Secrets.** Google client id and secret, the NextAuth secret and the API token key, kept in AppHost user secrets.

Several things only work in Development today:

- `src/Web/Program.cs` migrates the three DbContexts (Families, Catalog, Trips) and runs `DevelopmentSeeder`, which seeds the catalog and then the trips, only in Development.
- `src/ServiceDefaults/Extensions.cs` maps `/health` and `/alive` only in Development.
- The DbContexts use `UseSqlServer` without retries.
- The frontend has no production image. There is no Dockerfile and no `standalone` output.
- There is no CI at all: `.github/` holds only Copilot files.

Other constraints:

- The repository is `github.com/iverburgh/PakEchtMee`.
- The user chose:
  - the Azure stack unchanged;
  - strictly €0, so GHCR instead of ACR;
  - in production, only the general catalog as seed;
  - the default `*.azurecontainerapps.io` address.

## Goals / Non-Goals

**Goals:**

- One workflow file deploys every green push on `main`.
- Hand-written Bicep describes everything in a single resource group.
- Local development through Aspire stays exactly as it is.

**Non-Goals:**

- No custom domain, no staging environment and no pull-request preview environments.
- No zero-downtime guarantees beyond what Container Apps revisions give by default.
- No backup policy beyond Azure SQL's built-in point-in-time restore.
- No Application Insights. OpenTelemetry export stays off, and console logs go to Log Analytics.
- Deployment does not go through Aspire's publisher (`aspire publish` / `aspire deploy` / `azd`). See D1.

## Decisions

### D1. Hand-written Bicep instead of `aspire publish` / `azd`

**Decision.** Write the Bicep in `infra/` by hand.

**Why.** Aspire's Azure Container Apps publisher always provisions an Azure Container Registry (Basic, about €5 per month) and wires the apps to it with a managed identity. Avoiding it would mean post-processing generated Bicep on every publish. The resource set is small: an environment, two apps, a SQL server with one database, an identity and a workspace. Hand-written Bicep keeps it explicit and reviewable, and the AppHost stays a pure local orchestrator.

**Alternatives.**

- **`azd up` with Aspire.** Least code, but it brings ACR and its cost. Rejected because of the €0 requirement.
- **`aspire publish -p azure`, then patching.** Brittle across Aspire upgrades.

### D2. Images in GitHub Container Registry, private, pulled with a read-only token

**Images.** `ghcr.io/iverburgh/pakechtmee-api` and `ghcr.io/iverburgh/pakechtmee-web`, tagged with the commit SHA.

**Pushing.** The pipeline pushes with the workflow's own `GITHUB_TOKEN` (`packages: write`).

**Pulling.** Container Apps pulls with a classic personal access token that only has `read:packages`. The token is stored as GitHub secret `GHCR_PULL_TOKEN` and passed to Bicep as a secure parameter, which turns it into a Container Apps registry secret.

**Why private.** The image contains the compiled app and `appsettings.json`, which holds the administrator e-mail. That is no secret, but it should not be published needlessly.

**Alternative.** Public packages would need no token. That is a one-click change later if the token becomes a burden.

**Risk.** An expiring token stops new revisions from pulling (see Risks).

### D3. Container Apps: one Consumption environment, two apps, scale to zero, single replica

**Environment.**

- A Consumption-only Container Apps environment in `swedencentral`. `westeurope` and `northeurope` were tried first, but on the target subscription they accept no new customers or no new SQL servers; the apps and the database stay in one region so queries never cross regions.
- Its logs go to a Log Analytics workspace with a daily cap of 0.15 GB, about 4.5 GB per month, so it stays under the free 5 GB.

**`web` (Next.js).**

- External ingress on port 3000, HTTPS only: `allowInsecure: false` makes the platform redirect plain HTTP.
- `minReplicas: 0`, `maxReplicas: 1`.
- 0.25 vCPU and 0.5 GiB.

**`api` (.NET).**

- Internal ingress only, so it gets no public FQDN.
- `minReplicas: 0`, `maxReplicas: 1`.
- 0.25 vCPU and 0.5 GiB.
- Health probes on `/health` (readiness) and `/alive` (liveness).

**Why a single replica.** It makes migrations at startup safe without a distributed lock (see D6), and a family app never needs more.

**How the apps find each other.** The frontend reaches the API through `API_BASE_URL=http://api`. Container Apps resolves app names inside the environment, and internal traffic between apps is free and not counted as billable requests.

**Production `NEXTAUTH_URL`.** Bicep computes it from the environment's `defaultDomain` as `https://web.<defaultDomain>`, so it is known before the first deployment finishes. The Google redirect URI follows from it as `…/api/auth/callback/google`.

### D4. Azure SQL free offer with managed-identity access

**Server.**

- A logical SQL server with Microsoft Entra-only authentication. Its Entra administrator is a user-assigned managed identity `id-pakechtmee-api`.
- The identity is attached to the `api` app, so no SQL password exists anywhere.

**Network.** A firewall rule "Allow Azure services" (`0.0.0.0`) lets the API in. Nothing else may connect.

**Database `pakechtmee`.**

- `sku: GP_S_Gen5_2` with `useFreeLimit: true` and `freeLimitExhaustionBehavior: 'AutoPause'`.
- `autoPauseDelay: 60`, the minimum.

**Connection string.**

- Format: `Server=tcp:<server>.database.windows.net,1433;Database=pakechtmee;Authentication=Active Directory Managed Identity;User Id=<identity clientId>;Encrypt=True`.
- It is passed as `ConnectionStrings__pakechtmee`, the name the Web project already reads through Aspire.
- `Microsoft.Data.SqlClient` supports this authentication mode natively, so no new package is needed.

**Why the identity is server admin.** It is the simplest least-moving-parts setup for a single-app database. Creating a contained database user would need a post-deployment T-SQL step with an Entra-capable principal.

**Alternative.** A SQL login with a generated password. Rejected because it adds a secret to rotate.

### D5. GitHub Actions with OIDC

**Workflow.** `.github/workflows/deploy.yml` triggers on `push` to `main` and on `workflow_dispatch`. It sets `concurrency: { group: deploy-production, cancel-in-progress: false }`, so deployments queue in order instead of overlapping.

**Job `verify`** (ubuntu-latest):

- `dotnet build -warnaserror`.
- `dotnet test`. The Testcontainers tests run on the runner's Docker.
- `npm ci`, `npm run lint` and `npm run build` in `src/web-client`.

**Job `deploy`** (`needs: verify`, `environment: production`). Its permissions are `id-token: write`, `contents: read` and `packages: write`.

1. **API image.** `dotnet publish src/Web -t:PublishContainer` with `ContainerRegistry=ghcr.io` and `ContainerRepository=iverburgh/pakechtmee-api`. This uses the SDK's built-in container support, so no Dockerfile is needed for the API.
2. **Web image.** `docker build` of `src/web-client/Dockerfile`, then push.
3. **Sign in.** `azure/login` with `client-id`, `tenant-id` and `subscription-id`, which are not secrets but are stored as variables. Login uses a federated credential, so no Azure secret exists.
4. **Deploy.** `az deployment group create -g rg-pakechtmee -f infra/main.bicep` with the image tags and the secure parameters taken from secrets: `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `NEXTAUTH_SECRET`, `API_TOKEN_KEY` and `GHCR_PULL_TOKEN`.
5. **Report.** Print the frontend URL from the deployment outputs.

**Why the secrets are passed as `@secure()` parameters.** They are not logged and end up as Container Apps secrets, so they never land in environment-variable plain text in the portal.

**Service principal role.** Contributor on the resource group only. Bicep creates no role assignments: GHCR needs no AcrPull, and the SQL admin is set as a server property. Contributor is therefore enough.

### D6. Startup: migrate, seed the catalog, then serve

`Program.cs` changes as follows:

- **Every environment:** migrate Families, Catalog and Trips, then run `CatalogSeeder` with `CatalogSeedData.Categories`.
- **Development only:** run the trip part of `DevelopmentSeeder`.

To get there, `DevelopmentSeeder` is split so its catalog step can run on its own, or `Program.cs` calls `CatalogSeeder` directly and `DevelopmentSeeder` keeps only the trips. Whichever keeps the existing seeder tests simplest is picked at implementation time. That choice has no behavioral impact.

Why this order is safe:

- Migrations run before `app.Run()`, so the readiness probe only succeeds afterwards.
- With `maxReplicas: 1` there is no concurrent migrator.
- The existing `Seeding never overwrites user data` guarantees make the catalog seed safe to run on every start.

### D7. Resilience against a paused database

The three `UseSqlServer` calls get `sqlOptions.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: 30s)`. A database resuming from auto-pause answers with transient errors (40613) for up to about a minute, and the retry strategy rides that out.

`ExecutionStrategy` forbids user-initiated transactions outside `strategy.ExecuteAsync`. Implementation must check that no repository opens one explicitly; a quick check found none.

Migrations at startup run under the same strategy, because `MigrateAsync` uses it.

### D8. Health endpoints in every environment

`MapDefaultEndpoints` maps `/health` and `/alive` regardless of environment. They stay `AllowAnonymous` and only report status, which the `access-control` spec already allows ("Health check stays open"). The API is internal-only anyway (D3).

### D9. Frontend production image

`next.config.ts` gets `output: "standalone"`. `src/web-client/Dockerfile` is a multi-stage build:

- **Build stage.** `node:22-alpine` runs `npm ci` and `next build`. Serwist generates `public/sw.js` during the build.
- **Runtime stage.** Copies `.next/standalone`, `.next/static` and `public`, then runs as a non-root user with `node server.js` on `PORT=3000`.

The secrets come only from runtime environment variables, so no secret reaches the image. Server code reads `process.env` at request time. Nothing secret uses `NEXT_PUBLIC_`.

### D10. Environment configuration of the API in production

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__pakechtmee` | See D4 |
| `Auth__TokenKey` | Secret |
| `AZURE_CLIENT_ID` | The identity's client id |

`Access:Administrators` stays in `appsettings.json`.

## Risks / Trade-offs

- **[Cold start]** After idle time the visitor waits for three things in series: the web container, the API container, and a database resume of up to about a minute. → The spec accepts a slower first request. Retries (D7) prevent errors. The PWA shell keeps the app feeling responsive.
- **[Free database amount runs out]** The database pauses until the 1st of the next month, and the app then shows errors. 100,000 vCore-seconds is about 27 hours at 1 vCore. Every use keeps the database awake for at least the 60-minute auto-pause delay. → Light family use fits. The setup document advises an Azure alert on "Free amount remaining" below 10%.
- **[GHCR pull token expires]** A new revision cannot pull, and the old revision keeps running. → Create the classic token without expiry, or with a calendar reminder. The setup document describes how to rotate it: update the GitHub secret, then re-run the workflow. Switching the packages to public removes the token entirely.
- **[Free offer limits change]** Microsoft could change the free grants. → The Bicep parameters make SKU and limits explicit, so a change is a one-line edit. Expected costs are noted in the setup document.
- **[Migration at startup fails]** The new revision never gets ready, and Container Apps keeps the previous revision serving. → The failure shows in the logs. The fix goes out with the next push.
- **[Entra admin is the app identity]** The API has full rights on the database server. → Acceptable for a single-app database with no other users. A contained user can be introduced later without changing the specs.
- **[Testcontainers on GitHub runners]** SQL Server containers make `verify` slower, roughly several minutes. → Acceptable. The assembly fixture already shares one container per test assembly.

## Migration Plan

### One-time setup (`docs/azure-hosting.md`)

1. Create resource group `rg-pakechtmee` (the group itself sits in `westeurope`; its resources go to `swedencentral`, see D3).
2. Create an Entra app registration with a federated credential for `repo:iverburgh@56253492/PakEchtMee@1375801848:environment:production` (GitHub presents owner and repository ids in the subject), and give it Contributor on the resource group.
3. Create the GitHub environment `production`:
   - variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`;
   - secrets `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `NEXTAUTH_SECRET` (new random value), `API_TOKEN_KEY` (new random value) and `GHCR_PULL_TOKEN`.
4. **Bootstrapping the first push.** GHCR creates a package on its first push, and the deployment then pulls it with the token. If the first pull fails because the token cannot see the new package yet, re-run the job once.

### First deployment and after

5. Push to `main` and check the job output for the frontend URL.
6. Add `https://web.<defaultDomain>/api/auth/callback/google` as an authorised redirect URI on the existing Google OAuth client, and add the URL as an authorised JavaScript origin.
7. Sign in as the administrator and check that the general catalog and family Verburgh exist.

### Rollback

- Container Apps keeps earlier revisions, so activating the previous one in the portal or with `az containerapp revision activate` is a rollback.
- Reverting the commit on `main` redeploys the previous code.
- Database migrations are forward-only. A destructive migration needs its own plan, as before.

## Open Questions

- Whether to switch the GHCR packages to public later and drop the pull token. This can be decided after the first months of use without changing the specs.
