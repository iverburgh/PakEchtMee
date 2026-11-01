# Spec Delta

## REMOVED Requirements

### Requirement: Seeding runs only in the development environment

**Reason**: Production now needs the general catalog from the family packing list as well; only the example trips stay Development-only.
**Migration**: Replaced by "Catalog is seeded in every environment, trips only in development" below; a fresh production database receives the general catalog at API startup.

## ADDED Requirements

### Requirement: Catalog is seeded in every environment, trips only in development

The system SHALL apply the seed data at API startup, after the database migrations have been applied. The general catalog from the family packing list SHALL be seeded in every environment. The two seeded trips SHALL be seeded only in the Development environment.

#### Scenario: Seed applied on development startup

- **WHEN** the API starts in the Development environment against an empty database
- **THEN** after startup the catalog contains the seeded categories and items and the trip list contains the two seeded trips

#### Scenario: Only the catalog outside development

- **WHEN** the API starts in an environment other than Development against an empty database
- **THEN** the general catalog contains the seeded categories and items, and no seed trips are inserted
