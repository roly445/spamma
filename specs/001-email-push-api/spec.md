# Feature Specification: Developer First Push API

**Feature Branch**: `001-email-push-api`  
**Created**: November 10, 2025  
**Status**: Draft  
**Input**: User description: "Developer first push API. I want a push API that users can subscribe to in order to get notifications that emails have been received. this api needs to send the bare minimum of information, to, from, subject and the ID. Then there will be a get endpoint that the user can hit to get the get the mimemessage itself as an eml file. This API needs to be in an open standard consumable from any platform and it needs to be authenticated with JWTs. this is meant to be real time, so no webhooks. the user can only get emails from the subdomains they are viewers of. When they set up and integration they can either get post notifications for all a subdomain's emails, a single email, or provide a regex."

## Clarifications

### Session 2025-11-10

- Q: What is the open standard protocol for push notifications? → A: gRPC
- Q: How are push integrations identified and managed? → A: Each push integration has a unique GUID identifier per user, can be created, updated, and deleted via API endpoints.
- Q: What is the notification endpoint? → A: Server's gRPC endpoint for client connections.
- Q: Data volume assumptions? → A: unlimited.
- Q: Observability requirements? → A: no logs
- Q: What compliance or regulatory constraints apply to this feature? → A: None

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Set up Email Push Integration (Priority: P1)

As a developer, I want to configure a push integration for email notifications so that I can receive real-time alerts when emails arrive in subdomains I have access to.

**Why this priority**: This is the core functionality that enables the push API feature and allows developers to start receiving notifications.

**Independent Test**: Can be fully tested by creating an integration configuration and verifying it persists without requiring email reception.

**Acceptance Scenarios**:

1. **Given** a developer with viewer access to a subdomain, **When** they create a push integration for all emails in that subdomain, **Then** the integration is saved and ready to send notifications.
2. **Given** a developer with viewer access to a subdomain, **When** they create a push integration for a specific email address with regex pattern, **Then** the integration is saved with the regex filter.
3. **Given** a developer without viewer access to a subdomain, **When** they attempt to create a push integration, **Then** they receive an access denied error.
4. **Given** an existing push integration, **When** the developer updates the filter criteria, **Then** the integration is updated successfully.
5. **Given** an existing push integration, **When** the developer deletes it, **Then** the integration is removed and no further notifications are sent.

---

### User Story 2 - Receive Email Push Notifications (Priority: P1)

As a developer, I want to receive real-time push notifications when emails arrive in my configured integrations so that I can be notified immediately.

**Why this priority**: This delivers the primary value of real-time email notifications to developers.

**Independent Test**: Can be fully tested by simulating email arrival and verifying notification delivery to the configured endpoint.

**Acceptance Scenarios**:

1. **Given** an active push integration for all emails in a subdomain, **When** an email arrives in that subdomain, **Then** a push notification is sent with to, from, subject, and ID.
2. **Given** an active push integration with regex filter, **When** an email arrives matching the regex, **Then** a push notification is sent.
3. **Given** an active push integration, **When** an email arrives not matching the filter criteria, **Then** no push notification is sent.

---

### User Story 3 - Fetch Full Email Content (Priority: P2)

As a developer, I want to retrieve the full email content using the ID from a push notification so that I can access the complete message.

**Why this priority**: This provides the secondary functionality to get full email details after receiving the notification.

**Independent Test**: Can be fully tested by requesting an email by ID and verifying the EML file is returned.

**Acceptance Scenarios**:

1. **Given** a valid email ID from a push notification, **When** I request the full email content, **Then** I receive the MIME message as an EML file.
2. **Given** an invalid email ID, **When** I request the full email content, **Then** I receive a not found error.
3. **Given** an email ID from a subdomain I don't have viewer access to, **When** I request the full email content, **Then** I receive an access denied error.

---

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when the push notification delivery fails (network issues, endpoint unavailable)?
- How does the system handle concurrent email arrivals for multiple integrations?
- What happens when a regex pattern is invalid or causes performance issues?
- How does the system behave when JWT tokens expire during active integrations?
- What happens when an email arrives for a subdomain that has no active integrations?
- What happens when gRPC connection fails or is interrupted?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to create push integrations specifying notification scope (all emails, single email, or regex pattern) for subdomains they have viewer access to.
- **FR-002**: System MUST validate JWT authentication for all push API requests.
- **FR-003**: System MUST send real-time push notifications containing only to, from, subject, and email ID when emails arrive matching integration criteria.
- **FR-004**: System MUST provide a GET endpoint that returns the full MIME message as an EML file when given a valid email ID.
- **FR-005**: System MUST enforce subdomain viewer permissions, preventing access to emails outside user's authorized subdomains.
- **FR-006**: System MUST support gRPC protocol for push notifications consumable from any platform.
- **FR-007**: System MUST validate regex patterns in integration configurations to prevent invalid or malicious patterns.
- **FR-008**: System MUST handle integration failures gracefully without affecting email processing.
- **FR-009**: System MUST allow users to update and delete existing push integrations.

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero warnings. Any new warnings must be addressed before merging.
- **CQ-002**: Public types MUST use clear intent naming. XML documentation comments (`///`) are NOT required and MUST NOT be used for public API intent — code clarity and naming SHOULD provide intent.
- **CQ-003**: Blazor components MUST be split into a `.razor` file and a `.razor.cs` code-behind file for interactive logic; server-side-only static pages are exempt when explicitly marked.
- **CQ-004**: Commands and Queries types MUST be declared in the respective `.Client` project. Their handlers, validators and authorizers MUST be implemented in the non-`.Client` server project.
- **CQ-005**: No new project (csproj) is to be added to the repository without explicit approval from maintainers (documented in the PR).

## Key Entities *(include if feature involves data)*

- **Push Integration**: Represents a developer's subscription configuration, including subdomain, filter criteria (all/single/regex). Each integration has a unique GUID identifier per user and supports create, update, and delete operations.
- **Email Notification**: Contains minimal email metadata (to, from, subject, ID) sent in push notifications.
- **Email Content**: Full MIME message stored as EML file, accessible via ID.

## Assumptions

- Data volume: Unlimited integrations per user and emails per subdomain (performance and abuse prevention to be handled via operational limits).
- Observability: No logging required for push notifications (metrics and monitoring to be handled at infrastructure level).
- Compliance: None required for this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can set up push integrations in under 5 minutes from start to receiving first notification.
- **SC-002**: Push notifications are delivered within 1 second of email receipt for 99% of messages.
- **SC-003**: System supports 100 concurrent push integrations without performance degradation.
- **SC-004**: 95% of developers successfully retrieve full email content after receiving push notifications.
- **SC-005**: Zero security incidents related to unauthorized email access through the push API.
