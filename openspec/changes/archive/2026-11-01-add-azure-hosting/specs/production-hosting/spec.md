# Spec Delta

## Purpose

Defines how PakEchtMee runs online for the families: every change on `main` reaches production automatically once it passes its checks, the hosting stays within free allowances, and production holds no secrets in the repository.

## ADDED Requirements

### Requirement: Every push to main is deployed after its checks pass

Each push to the `main` branch SHALL trigger a pipeline that first builds the backend and frontend and runs all automated tests and the frontend lint. It SHALL deploy to production only when every check passes. A failed check SHALL leave the running production version untouched.

#### Scenario: Green push is deployed

- **WHEN** a commit is pushed to `main` and all builds, tests and lint pass
- **THEN** the pipeline deploys that commit and production serves it afterwards

#### Scenario: Failing test blocks the deployment

- **WHEN** a commit is pushed to `main` and a test fails
- **THEN** no deployment step runs and production keeps serving the previous version

#### Scenario: Other branches are not deployed

- **WHEN** a commit is pushed to a branch other than `main`
- **THEN** no deployment takes place

### Requirement: Deployments do not overlap

When pushes to `main` follow each other quickly, the system SHALL deploy them one at a time in push order. Production SHALL end up running the latest pushed commit that passed its checks.

#### Scenario: Two quick pushes

- **WHEN** two commits are pushed to `main` shortly after each other and both pass their checks
- **THEN** their deployments do not run at the same time, and production ends on the second commit

### Requirement: The whole environment is reproducible from the repository

All Azure resources production needs SHALL be described in the repository and created or updated by the pipeline. This includes hosting, database, identity and logging. Apart from the documented one-time setup, nothing SHALL have to be created by hand. Deploying an unchanged description twice SHALL leave the resources unchanged.

#### Scenario: First deployment on an empty resource group

- **WHEN** the one-time setup is done and the pipeline runs against an empty resource group
- **THEN** all resources are created and the app is reachable on its public address

#### Scenario: Repeated deployment

- **WHEN** the pipeline runs again without changes to the resource description
- **THEN** no resource is recreated or loses its data

### Requirement: Hosting stays within free allowances

Production SHALL scale the frontend and API to zero instances when idle. It SHALL use a database tier that pauses instead of billing once its free monthly amount is used. It SHALL store images in a registry that costs nothing, and cap log ingestion within the free logging amount.

#### Scenario: Idle app

- **WHEN** nobody uses the app for a while
- **THEN** no frontend or API instance keeps running

#### Scenario: Free database amount used up

- **WHEN** the database has used its free monthly amount
- **THEN** the database pauses until the next month instead of incurring charges

### Requirement: The app resumes after being idle

The first request after an idle period SHALL be served correctly once the frontend, API and database have resumed. It SHALL NOT fail merely because the database was paused. A cold start MAY take noticeably longer than a normal request.

#### Scenario: First visit after a quiet week

- **WHEN** a member opens the app after days without any use
- **THEN** the page loads, possibly slowly, and shows the member's data without an error

### Requirement: Only the frontend is reachable from the internet

The frontend SHALL be served over HTTPS on a public address. The API SHALL be reachable only from the frontend inside the hosting environment. The database SHALL accept connections only from Azure services, using an identity managed by the platform instead of a password.

#### Scenario: API not exposed

- **WHEN** someone on the internet tries to call the API directly
- **THEN** there is no public address that reaches it

#### Scenario: Plain HTTP

- **WHEN** someone opens the frontend over `http://`
- **THEN** they are redirected to the HTTPS address

### Requirement: Production secrets never live in the repository

The production secrets SHALL be kept in the CI system's secret store and handed to the running apps as platform secrets. They SHALL NOT appear in the repository, in images or in pipeline logs. These are the Google client id and secret, the session secret, the API token key and the registry pull token. The pipeline SHALL sign in to Azure without a stored password or key.

#### Scenario: Image contents

- **WHEN** a published image is inspected
- **THEN** it contains no production secret

#### Scenario: Pipeline log

- **WHEN** the deployment log is read
- **THEN** no secret value is printed

### Requirement: Production database is migrated and seeded at startup

At API startup in every environment, the system SHALL apply pending database migrations before serving requests. It SHALL then make sure the general catalog from the family packing list is present. A fresh production database SHALL thus offer family "Verburgh" with the administrator as member, and the general catalog, without manual steps.

#### Scenario: First start on an empty production database

- **WHEN** the API starts for the first time against an empty production database
- **THEN** the schema exists, family "Verburgh" with `ivoverburgh@gmail.com` exists, and the general catalog holds the seeded categories and items

#### Scenario: Startup after a new migration

- **WHEN** a deployment contains a new migration
- **THEN** the API applies it at startup and serves requests only afterwards

### Requirement: Health endpoints are available in production

The API SHALL expose its health endpoints in every environment, without authentication and without exposing data, so the platform can probe readiness and liveness.

#### Scenario: Platform probe

- **WHEN** the hosting platform calls the API's health endpoint in production
- **THEN** it receives the health status without a token
