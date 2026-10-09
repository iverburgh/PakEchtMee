# Spec Delta

## Purpose

Defines how the web client behaves as a mobile-first progressive web app, so that it can be installed on any modern mobile device and stays usable one-handed while packing.

## ADDED Requirements

### Requirement: Installable as a progressive web app

The web client SHALL be installable on modern mobile browsers. It SHALL publish a web app manifest containing an application name, a short name, a start URL, `standalone` display mode, a theme colour, a background colour, and maskable icons of at least 192x192 and 512x512 pixels. When launched from the home screen the application SHALL open without browser address bar chrome.

#### Scenario: Installing on a mobile device

- **WHEN** a user visits the application in a browser that supports installation
- **THEN** the browser offers to install the application and the installed app appears with its own name and icon

#### Scenario: Launching the installed app

- **WHEN** a user launches the installed application from the home screen
- **THEN** the application opens full screen in standalone mode at the start URL

### Requirement: App shell available without a network connection

The application SHALL serve its application shell from a service worker cache, so that launching it without a network connection renders the interface instead of a browser error page. When data cannot be retrieved, the system SHALL show an explicit offline state that names the situation and offers a retry.

#### Scenario: Launching without connectivity

- **WHEN** a user launches the installed application with no network connection
- **THEN** the application shell renders and shows an offline state with a retry action, not a browser error page

#### Scenario: Reconnecting

- **WHEN** connectivity returns and the user retries
- **THEN** the current screen loads its data and the offline state disappears

### Requirement: Changes require connectivity and never silently fail

Creating or changing data SHALL require a network connection. When a change cannot be sent, the system SHALL keep the interface in its last known correct state, SHALL report the failure to the user, and SHALL NOT present the change as saved.

#### Scenario: Status change while offline

- **WHEN** a user advances an item's status without a network connection
- **THEN** the item visibly returns to its previous status and the user is told the change was not saved

### Requirement: Mobile-first interaction

The application SHALL be designed for a phone screen first and SHALL remain usable from 320 CSS pixels wide upwards, without horizontal scrolling. Primary actions in the item walkthrough SHALL have a touch target of at least 44x44 CSS pixels and SHALL be reachable with one thumb. Layouts SHALL adapt to larger screens without losing any functionality, and SHALL respect the operating system's reduced-motion preference.

#### Scenario: Narrow phone screen

- **WHEN** the application is opened on a 320 pixel wide viewport
- **THEN** all content fits without horizontal scrolling and the primary actions remain tappable

#### Scenario: Touch target size

- **WHEN** a user taps the advance action of an item on a phone
- **THEN** the tappable area is at least 44x44 CSS pixels

#### Scenario: Reduced motion

- **WHEN** the device requests reduced motion
- **THEN** transitions and animations are reduced or removed while all functionality remains available
