# Tasks

## 1. API production readiness

- [x] 1.1 Map `/health` and `/alive` in every environment in `ServiceDefaults` (design D8). Verify with a Web test against a `Production`-environment factory: `/health` without token returns 200.
- [x] 1.2 Add `EnableRetryOnFailure` to the three `UseSqlServer` registrations (design D7). Check that no repository starts an explicit transaction outside the execution strategy. Verify with `dotnet test`: all infrastructure integration tests still pass.
- [x] 1.3 Make startup migrate all three contexts and seed the general catalog in every environment, and seed the trips only in Development (design D6, `development-seed-data` delta). Verify with Web tests: on an empty database in a `Production` factory the general catalog exists and no trips do; the existing Development seed tests still pass unchanged.
- [x] 1.4 Enable SDK container publishing for `src/Web` (`ContainerRepository`, `ContainerImageTag`, non-root base image). Verify `dotnet publish src/Web -t:PublishContainer -p:ContainerRegistry=` builds a local image, and that the container starts and answers `/alive` when given a connection string.

## 2. Frontend production image

- [x] 2.1 Set `output: "standalone"` in `next.config.ts` and add `src/web-client/Dockerfile` plus `.dockerignore` (multi-stage, non-root, `PORT=3000`; design D9). Verify:
  - `docker build` succeeds;
  - the container serves `/inloggen` with the service worker present;
  - `docker history` and the image filesystem contain no `.env` or secret values;
  - `npm run build` still passes and `aspire run` still works locally.

## 3. Azure infrastructure (Bicep)

- [x] 3.1 Write `infra/main.bicep` with these resources and parameters (design D3, D4, D10):
  - resources: the Log Analytics workspace with a 0.15 GB daily cap, the user-assigned identity, the SQL server (Entra-only, identity as admin, Azure-services firewall rule), and the free-offer database with `AutoPause`;
  - the Consumption Container Apps environment, the `api` app (internal ingress, probes, identity, 0–1 replicas) and the `web` app (external HTTPS-only ingress, 0–1 replicas, `NEXTAUTH_URL` derived from `defaultDomain`);
  - a GHCR registry secret;
  - `@secure()` parameters for every secret;
  - outputs: the frontend URL and the redirect URI.

  Verify with `az bicep build` (no warnings) and `az deployment group what-if` against the target resource group.
- [ ] 3.2 Write `docs/azure-hosting.md` in Dutch, covering the one-time setup from the design's Migration Plan:
  - the resource group;
  - the app registration with its federated credential and the Contributor role;
  - the GitHub environment `production` with its variables and secrets;
  - the classic GHCR token with `read:packages`;
  - the Google redirect URI and JavaScript origin;
  - the free-amount alert;
  - token rotation and rollback.

  Verify by following it once with the user, until `az deployment group what-if` runs with the service principal.

## 4. GitHub Actions pipeline

- [ ] 4.1 Add `.github/workflows/deploy.yml` (design D5):
  - triggers on push to `main` and on `workflow_dispatch`;
  - concurrency group `deploy-production` without cancel-in-progress;
  - job `verify`: .NET build with warnings as errors, `dotnet test`, and `npm ci`, lint and build;
  - job `deploy` (needs `verify`, environment `production`, OIDC login): pushes both images tagged with the commit SHA to GHCR, runs `az deployment group create`, and prints the frontend URL.

  Verify the following:
  - a push with a deliberately failing test on a throwaway commit stops before `deploy`, and is reverted afterwards;
  - a green push deploys;
  - the job log shows no secret values.

## 5. End-to-end check

- [ ] 5.1 After the first green deployment, add the redirect URI to Google and check production end to end:
  - the frontend URL loads over HTTPS, and plain `http://` redirects;
  - the API has no public FQDN;
  - signing in as the administrator shows family Verburgh and the general catalog of 22 categories, and no seed trips;
  - after the apps have scaled to zero, the first visit loads without an error.
- [ ] 5.2 Push a trivial change to `main` and verify it is live without manual steps. Push two commits in quick succession and verify the deployments ran one after the other and production serves the second.

## Workflow follow-up

- Archive with `/opsx:archive` after review. Per project preference, archiving is followed by commit and push.
