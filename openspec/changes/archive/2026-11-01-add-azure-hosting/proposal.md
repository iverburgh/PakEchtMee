# Proposal

## Why

PakEchtMee only runs locally through Aspire, so the family cannot use it on holiday or on their own phones. It needs to run online at (practically) no cost, and every change on `main` must reach production without manual steps, so hosting never lags behind the code.

## What Changes

- Host the app on an Azure subscription with the current stack unchanged (.NET API, Next.js frontend, SQL Server):
  - Azure Container Apps on the Consumption plan, scaling to zero, for the API (internal ingress only) and the frontend (public HTTPS on the default `*.azurecontainerapps.io` address).
  - Azure SQL Database using the free offer, set to auto-pause when the monthly free amount runs out instead of billing.
  - Container images in GitHub Container Registry instead of Azure Container Registry, so there is no registry cost.
  - All Azure resources described as Bicep in the repository, so the whole environment can be recreated on any subscription.
- Add a GitHub Actions workflow that runs on every push to `main`:
  - It builds and tests everything first (.NET build and tests, frontend lint and build).
  - Only when that passes, it publishes the images, provisions or updates the Azure resources, and rolls out the new version.
  - It signs in to Azure with OpenID Connect (federated credentials), so no Azure password or key is stored in GitHub.
- Make the API production-ready:
  - Apply database migrations at startup in every environment.
  - Retry transient SQL errors, so a paused free database can resume.
  - Expose the health endpoints outside Development for the platform's probes.
  - Reach Azure SQL with a managed identity instead of a password.
- **BREAKING (seeding)**: the general catalog from the family packing list is now seeded in every environment, including production. The two example trips stay Development-only.
- Production secrets live in GitHub Actions secrets and are passed to Container Apps as secrets. These are the Google client id and secret, the NextAuth secret, the API token key and a read-only GHCR pull token.
- Document the one-time setup in `docs/azure-hosting.md`:
  - resource group, service principal with federated credential, GitHub secrets;
  - adding the production redirect URI to the Google OAuth client.

## Capabilities

### New Capabilities

- `production-hosting`: how PakEchtMee runs online, which covers:
  - automated deployment from `main` (only after tests pass);
  - zero-cost hosting constraints (scale to zero, free database that pauses instead of billing);
  - the API not being publicly reachable;
  - secrets never living in the repository;
  - the production database being migrated and given the general catalog at startup.

### Modified Capabilities

- `development-seed-data`: the requirement "Seeding runs only in the development environment" changes. The general catalog is now seeded in every environment, while the example trips remain Development-only.

## Impact

- **New files**
  - `infra/` (Bicep: Container Apps environment, two Container Apps, Azure SQL server and free database, Log Analytics, user-assigned managed identity).
  - `.github/workflows/deploy.yml`.
  - A `Dockerfile` for the Next.js frontend, plus Next.js `standalone` output.
  - `docs/azure-hosting.md`.
- **Changed code**
  - `src/Web/Program.cs`: migrations and the catalog seed in every environment, trips only in Development.
  - `src/ServiceDefaults/Extensions.cs`: health endpoints in every environment.
  - The three `ServiceCollectionExtensions` that register the DbContexts: `EnableRetryOnFailure`.
  - `src/Web/Web.csproj`: SDK container publishing.
  - `src/web-client/next.config.ts`.
- **Dependencies**
  - One new NuGet package: `Microsoft.Data.SqlClient.Extensions.Azure` (Entra ID sign-in for SqlClient 7, used for the managed identity). No new npm packages. The API image is built with the .NET SDK container support (no Dockerfile).
  - The pipeline uses GitHub-hosted runners, `azure/login`, Docker and the Azure CLI.
- **Local development** is unchanged: Aspire keeps running SQL Server in a container. The AppHost is not used for deployment.
- **External systems**
  - Azure subscription (resource group).
  - GitHub (Actions, Container Registry, secrets).
  - Google Cloud Console (production redirect URI).
- **Cost**: €0 expected within the free grants. Log Analytics ingestion is capped daily to stay within its free amount.
