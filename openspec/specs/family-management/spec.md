# family-management Specification

## Purpose
Lets the administrator organise people into families, so each family packs from its own catalog and trips while the administrator alone controls who belongs where.

## Requirements

### Requirement: Administrator manages families

The administrator SHALL be able to create a family with a non-empty name of at most 60 characters, unique among families (compared case-insensitively), to rename it, and to list all families with their members. Families SHALL NOT be deleted in this version.

#### Scenario: Create a family

- **WHEN** the administrator creates the family "Jansen"
- **THEN** "Jansen" appears in the family list without members, with an empty own catalog and no trips

#### Scenario: Duplicate family name is rejected

- **WHEN** the administrator creates a family named "verburgh" while "Verburgh" exists
- **THEN** the request is rejected with a validation error and no family is created

#### Scenario: Rename a family

- **WHEN** the administrator renames "Jansen" to "Familie Jansen"
- **THEN** the family keeps its members, catalog and trips under the new name

### Requirement: Administrator adds and removes members

The administrator SHALL be able to add a Google account to a family by its e-mail address and to remove a member from a family. E-mail addresses SHALL be stored in lower case and SHALL be syntactically valid. Adding SHALL NOT require the account to have signed in before.

#### Scenario: Add a member before first sign-in

- **WHEN** the administrator adds `oma.jansen@gmail.com` to "Jansen"
- **THEN** that account can sign in afterwards and sees the catalog and trips of "Jansen"

#### Scenario: Remove a member

- **WHEN** the administrator removes `oma.jansen@gmail.com` from "Jansen"
- **THEN** that account can no longer sign in, and the catalog and trips of "Jansen" are unchanged

#### Scenario: Invalid e-mail address

- **WHEN** the administrator adds `oma.jansen` to a family
- **THEN** the request is rejected with a validation error

### Requirement: An account belongs to at most one family

The system SHALL reject adding an e-mail address that is already a member of any family, and SHALL name the family it already belongs to. To move an account, the administrator SHALL first remove it from its current family.

#### Scenario: Account already in another family

- **WHEN** the administrator adds `martine@gmail.com` to "Jansen" while it is a member of "Verburgh"
- **THEN** the request is rejected with a validation error naming "Verburgh"

### Requirement: Only the administrator manages families and members

The system SHALL reject every request to create, rename or list families or to add or remove members when it does not come from an administrator, with status 403. The family pages SHALL NOT be offered to members who are not administrators.

#### Scenario: Member tries to add someone

- **WHEN** a member of "Verburgh" who is not an administrator tries to add `vriend@gmail.com` to "Verburgh"
- **THEN** the request is rejected with status 403 and no member is added

### Requirement: Administrator may also be a family member

The administrator SHALL be able to be a member of one family like any other account, and SHALL then use that family's catalog and trips while keeping the administrator pages.

#### Scenario: Administrator packs for their own family

- **WHEN** `ivoverburgh@gmail.com`, a member of "Verburgh", opens the trip list
- **THEN** the trips of "Verburgh" are shown and the administrator pages are available in the navigation
