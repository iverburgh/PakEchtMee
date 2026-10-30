# access-control Specification

## Purpose
Decides who may use PakEchtMee at all: people sign in with their Google account, only administrators and members of a family are let in, and no data is served to anyone whose identity has not been verified.

## Requirements

### Requirement: Sign in with Google

The system SHALL let people sign in only with a Google account whose e-mail address Google reports as verified. Every page except the sign-in page, the no-access page and the offline page SHALL require a signed-in account; a visitor without a session SHALL be sent to the sign-in page and returned to the requested page after signing in.

#### Scenario: Visitor is sent to sign in

- **WHEN** a visitor without a session opens `/catalogus`
- **THEN** the sign-in page is shown, and after a successful Google sign-in the visitor lands on `/catalogus`

#### Scenario: Unverified e-mail is refused

- **WHEN** someone signs in with a Google account whose e-mail address is not verified
- **THEN** the sign-in is refused and no session is created

### Requirement: Administrators can always sign in

The system SHALL treat every e-mail address in the configured administrator list as an administrator. The list SHALL contain `ivoverburgh@gmail.com`. An administrator SHALL be able to sign in whether or not the account belongs to a family. E-mail addresses SHALL be compared case-insensitively.

#### Scenario: Administrator without a family signs in

- **WHEN** `ivoverburgh@gmail.com` signs in while it is not a member of any family
- **THEN** the sign-in succeeds and the administrator pages are available

#### Scenario: Case does not matter

- **WHEN** Google reports the address as `IvoVerburgh@Gmail.com`
- **THEN** the account is recognised as the administrator

### Requirement: Unknown accounts are refused

The system SHALL refuse sign-in for an account that is neither an administrator nor a member of a family, and SHALL show a no-access page that names the e-mail address used and explains that an administrator has to add it.

#### Scenario: Stranger tries to sign in

- **WHEN** `someone@gmail.com`, who is not a member of any family, signs in with Google
- **THEN** no session is created and the no-access page shows `someone@gmail.com`

### Requirement: Losing access takes effect immediately

The system SHALL check the account's administrator status and family membership on every request that reads or changes data. When an account is removed from its family while signed in, its next request SHALL be refused and the person SHALL be shown the no-access page.

#### Scenario: Removed member is stopped on the next action

- **WHEN** a signed-in member is removed from the family and then opens the trip list
- **THEN** no trips are shown and the no-access page is displayed

### Requirement: Sign out

The system SHALL offer a sign-out action on every signed-in page. Signing out SHALL end the session on that device and show the sign-in page.

#### Scenario: Signing out

- **WHEN** a signed-in member chooses sign out
- **THEN** the sign-in page is shown and opening `/` again requires signing in

### Requirement: The API serves nothing without a verified identity

Every API endpoint except the health checks SHALL reject a request that does not carry a valid, unexpired identity token issued by the frontend, with status 401 and no data. A valid token for an account that is neither administrator nor member SHALL be rejected with status 403.

#### Scenario: Direct call without a token

- **WHEN** a client calls `GET /api/trips` on the API port without a token
- **THEN** the API responds with 401 and returns no trips

#### Scenario: Expired token

- **WHEN** a client calls the API with a token that expired a minute ago
- **THEN** the API responds with 401

#### Scenario: Health check stays open

- **WHEN** a client calls `/health` without a token
- **THEN** the API responds with its health status
