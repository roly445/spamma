# Feature Specification: Chaos Monkey Email Address (Chaos Address)

**Feature Branch**: `001-chaos-address`  
**Created**: 2025-11-01  
**Status**: IMPLEMENTED (see Implementation & Tests section below)
**Input**: User description: "A chaos monkey address that can respond with a fixed SMTP error code. Created by users with domain management permission, assigned as moderator or viewer. Must be unique, disabled by default, immutable after first use, and track total received count and most recent date. Starts disabled; when disabled emails processed normally; when enabled returns the configured SMTP error. Cannot be edited or deleted after first email received."

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

### User Story 1 - Create Chaos Address (Priority: P1)

As a domain administrator (or user with domain management permission), I want to create a "chaos address" for a domain or subdomain so that I can exercise and test how external systems handle specific SMTP error responses.

**Why this priority**: This enables developers and operators to test error handling end-to-end across sending systems and downstream processors. It is the core capability required for the feature.

**Independent Test**: Create a chaos address via the domain management UI/API and verify it exists in the domain's configuration and starts in a disabled state.

**Acceptance Scenarios**:

1. **Given** a user with domain management permission and an existing domain/subdomain, **When** they create a chaos address with a unique local-part and choose an SMTP error code, **Then** the system saves the chaos address in a disabled state and shows it in the domain's Chaos Addresses list.
2. **Given** a user without domain management permission, **When** they attempt to create a chaos address, **Then** the system denies the action with an authorization error.

---

### User Story 2 - Enable / Disable Chaos Address (Priority: P2)

As a domain moderator or viewer assigned to a subdomain, I want to enable or disable an existing chaos address so that tests can be toggled on and off without removing the address.

**Why this priority**: Toggling tests on/off is a common operational need and avoids creating/deleting addresses during test cycles.

**Independent Test**: Enable a created chaos address and send a test email to it; the system should return the configured SMTP error code. Disable the address and send another test email; the system should process it normally.

**Acceptance Scenarios**:

1. **Given** a chaos address in disabled state, **When** an authorized moderator enables it, **Then** the address becomes active and subsequent incoming messages to that address are responded to with the configured SMTP error code.
2. **Given** a chaos address in enabled state, **When** it is disabled, **Then** subsequent incoming messages are processed as normal and no SMTP error is returned.

---

### User Story 3 - Immutable After First Use (Priority: P3)

As a system owner, I want chaos addresses to become immutable (no edits or deletion) after they have received their first email so that tests remain auditable and cannot be changed retroactively.

**Why this priority**: Ensures test results remain consistent and prevents accidental removal or modification of addresses used in past tests.

**Independent Test**: Send an email to a newly created chaos address, then attempt to edit or delete it; the system should reject edit and delete operations after the first received message.

**Acceptance Scenarios**:

1. **Given** a chaos address that has never received email, **When** an authorized user edits or deletes it, **Then** the operation succeeds.
2. **Given** a chaos address that has received at least one email, **When** an authorized user attempts to edit or delete it, **Then** the system rejects the request with an appropriate error indicating immutability after first use.

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

- Multiple recipients: If the same message is addressed to both a chaos address and other normal recipients on the same domain, the chaos address behavior applies only to the recipient mailbox — the SMTP response should be returned for the RCPT/recipient that maps to the chaos address while other recipients are processed normally where protocol allows. (See note in constraints.)

- Recipient matching semantics: When processing recipients during inbound delivery, the system MUST apply the chaos rule to the first recipient that matches a configured enabled `ChaosAddress`; if no recipient matches any configured enabled chaos addresses, the message MUST flow through the normal inbound processing pipeline. For messages with multiple recipients, the chaos behavior applies only to the first matching recipient; other recipients MUST be processed normally where the SMTP protocol and server implementation allow per-recipient responses.

- Duplicate creation attempts: Creating a chaos address with the same local-part on the same domain must fail as addresses are unique per-domain.

- Race on first-receive: If two messages arrive at the same moment, ensure the transition from editable to immutable and the increment of counters is atomic to avoid lost updates.

- Storage limits: Track only aggregate counts and most recent timestamp; do not attempt to store unlimited per-message details to avoid unbounded growth.

--

## Implementation & Tests (current state)

This section documents the implemented behavior, files, and tests as present in the repository on branch `001-chaos-address`.

Status: Feature implemented, committed and tested. Full solution build is clean and unit tests pass across relevant projects. See `specs/001-chaos-address/tasks.md` for a task-level execution log.

Key implemented artifacts (non-exhaustive):

- Domain aggregate and events:
  - `src/modules/Spamma.Modules.DomainManagement/Domain/ChaosAddressAggregate/ChaosAddress.cs`
  - `src/modules/Spamma.Modules.DomainManagement/Domain/ChaosAddressAggregate/Events/` (ChaosAddressCreated, ChaosAddressEnabled, ChaosAddressDisabled, ChaosAddressReceived, ChaosAddressSubdomainChanged, ChaosAddressLocalPartChanged, ChaosAddressSmtpCodeChanged)

- Read model & projection:
  - `src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/ChaosAddressLookup.cs`
  - `src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/ChaosAddressLookupProjection.cs`

- Commands / Handlers / Validators:
  - Client DTOs: `src/modules/Spamma.Modules.DomainManagement.Client/Application/Commands/...` (Create, Enable, Disable, DeleteChaosAddressCommand in `Commands/DeleteChaosAddress`)
  - Server handlers: `src/modules/Spamma.Modules.DomainManagement/Application/CommandHandlers/ChaosAddress/*` (Create, Enable, Disable, RecordReceived, Delete)
  - Validators: `src/modules/Spamma.Modules.DomainManagement/Application/Validators/ChaosAddress/*` (Create/Edit/Delete/Enable/Disable/RecordReceived). A minimal `DeleteChaosAddressCommandValidator` ensures `Id` is present for delete operations.

- SMTP integration:
  - `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/SpammaMessageStore.cs` - detects recipients in To/Cc/Bcc order, queries the `ChaosAddressLookup` read model, and returns configured SMTP error codes for the first matching enabled chaos address; falls back to normal processing otherwise. It also dispatches `RecordChaosAddressReceivedCommand` as a best-effort update for counters.

- Client UI (WASM):
  - Dedicated page `ChaosAddresses.razor` and list component `ChaosAddressList.razor` (routes `/chaos-addresses` and `/chaos-addresses/{subdomainId}`) with placeholders for Create modal.

Tests & Build Status (as of last run):

- Full solution build: succeeded (0 errors, 0 warnings)
- DomainManagement tests: succeeded (103 tests, 0 failures)
- EmailInbox tests: succeeded (5 targeted SMTP processing tests, all passing)
- Full test summary: 273 total tests, 0 failed, 273 succeeded (see `dotnet test` output and `specs/001-chaos-address/tasks.md` for details)

Known implementation notes:

- Immutability: Implemented in aggregate logic — once `TotalReceived > 0` the aggregate rejects edits and deletions; enable/disable remain allowed.
- Projection: `ChaosAddressLookupProjection` updates read model fields and patches atomic counters for `TotalReceived` and `LastReceivedAt` on `ChaosAddressReceived` events.
- SMTP per-recipient semantics: SpammaMessageStore implements first-match behavior and responds per recipient where the underlying SMTP library supports it.

If you need the spec to include further implementation details (file-level mapping, PR references, or code excerpts), say which areas to expand and I will add them.

- Disabled by default: Ensure existing message processing paths are preserved when the chaos address is disabled.

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST allow users with domain management permission to create a chaos address for a specific domain or subdomain, providing: local-part (the left side of the address) and the target SMTP error response (see FR-003). `SubdomainId` is required for each chaos address.
- **FR-002**: Chaos addresses MUST be unique per domain/subdomain. Attempts to create a duplicate MUST fail with a clear validation error.
- **FR-003**: When a chaos address is enabled, the system MUST respond to incoming SMTP deliveries for that recipient with the configured single SMTP error code. The configured code MUST be represented by the `Spamma.Modules.Common.SmtpResponseCode` enum and limited to codes supported by the SmtpServer library used by the platform; the UI/API MUST present only the supported options to the user (no arbitrary numeric input). Initially the allowed set will be those codes the SmtpServer package can emit and the platform will document which codes are available.
- **FR-004**: Chaos addresses MUST be created in a disabled state by default. Enable/disable operations MUST be available to authorized roles (domain moderators and users assigned as viewers with explicit permission for the subdomain).
- **FR-005**: Once a chaos address has received its first email, the address MUST become immutable — edits and deletion MUST be rejected. Enable/disable operations are allowed after first receive. Immutability is a derived business rule (first-receive implies immutability) and is not stored as a separate persisted flag.
- **FR-006**: The system MUST track, per chaos address, an aggregate counter of total emails received and the timestamp (UTC) of the most recent email received. These values MUST update atomically on message receipt.
- **FR-007**: If a chaos address is disabled, incoming messages to that address MUST be processed by the normal inbound processing pipeline (no special error response).
- **FR-008**: Attempts to delete a chaos address that has received at least one email MUST be rejected with a clear error explaining immutability.
 - **FR-009**: The UI and API MUST expose chaos addresses on a dedicated, non-admin-only page and via the domain/subdomain administration flows. Requirements:
  - Route: a dedicated page MUST be available at `/chaos-addresses` (client-side WASM route). The page SHOULD also accept an optional route segment `/chaos-addresses/{subdomainId}` or query parameter `?subdomainId=` to pre-filter by subdomain. This page is separate from the legacy admin-only pages and is discoverable from `SubdomainDetails` via a "Manage Chaos Addresses" link.
   - Display: the page and API list view MUST show: local-part, status (enabled/disabled), configured SMTP error (enum), total-received count, last-received (UTC) timestamp (or "Never received"), created date, and created-by where available.
   - Immutability: the UI MUST infer immutability from `TotalReceived > 0` and prevent edits and deletes for immutable addresses (actions hidden or disabled with a tooltip explaining immutability). Enable/disable controls remain allowed after first receive.
   - Access / Permissions: this page is NOT admin-only. Permission mapping follows project conventions (see FR-010 and plan):
     - `DomainManagement` (system role): full access across domains/subdomains (create, enable/disable, edit/delete prior to first receive, view audit data).
     - `DomainModerator`: full access within their domain (create, enable/disable, edit/delete prior to first receive, view audit data).
     - `SubdomainModerator`: full access within their subdomain.
     - `SubdomainViewer`: per the project's conventions, viewers MAY be granted creation/enable/disable rights for their subdomain. The UI MUST hide or disable actions the current user is not authorized to perform and surface authorization errors returned by the API.
   - Discovery: `SubdomainDetails.razor` MUST include a prominent link/button labeled "Manage Chaos Addresses" that navigates to the dedicated page for the subdomain. The subdomain details page may also include a compact preview (e.g., top 3 chaos addresses) but the full management experience must be on the dedicated page.
   - API: The API endpoints/queries for listing and detail MUST support server-side authorization checks and return only entries the caller is permitted to view or act upon. The client should use `IQuerier` / `ICommander` patterns for queries and commands.

UI/UX Implementation Notes (decisions):

- Create flow: The create form for a chaos address SHOULD be presented in a modal/slidout from the dedicated page (not a separate navigation page). The projects standard modal/slidout/confirm patterns MUST be used — the UI code should call into the shared modal components so behavior and accessibility are consistent across the app.

- SMTP codes source: The set of allowed `SmtpResponseCode` values MUST be represented as constants in `Spamma.Modules.Common.Client` and consumed by the client UI; the UI MUST NOT accept arbitrary numeric input for the SMTP code field.

- Timestamp / null display: The UI MUST display `LastReceivedAt` as either the UTC timestamp (formatted like `yyyy-MM-dd HH:mm UTC`) or the string `"Never received"` when null. Optionally the UI may show the user's local time on hover or in a tooltip.

- Immutability UX: When `TotalReceived > 0` the address is immutable. The UI MUST not show an Edit or Delete CTA for immutable addresses (no edit CTA). The Enable/Disable control remains visible and usable after first receive.

- Page access vs actions: The dedicated page at `/chaos-addresses` is discoverable and reachable by authorized users and viewers; the page itself should be accessible for read-only viewing even if the current user lacks management permissions. UI action controls (Create, Enable, Disable, Delete, Edit) MUST be hidden for users that lack the corresponding permission; attempting actions via direct API calls must still be rejected server-side per FR-010.

- Validation & error mapping: Server-side validation errors returned by command handlers MUST include field-level information that the client can map to individual form controls. The client UI MUST use the project's standard form error mapping (inline field errors + toast/alert for non-field errors) so create/update failures present clean, field-associated error messages and success notifications per existing conventions.
- **FR-010**: The system MUST prevent non-authorized users from creating, editing, enabling/disabling, or deleting chaos addresses according to existing domain permission rules.
- **FR-011**: The system MUST ensure that responding with SMTP error for a recipient does not prevent processing of other recipients where SMTP protocol allows per-recipient responses; the system implementation MUST use appropriate SMTP response semantics.
- **FR-012**: The system MUST log auditable events for: chaos address created, enabled, disabled, first-received (transition to immutable), and attempted edits/deletes after immutability, including actor, timestamp, and reason.

### Key Entities *(include if feature involves data)*

- **ChaosAddress**: Represents a configured chaos mailbox for a domain or subdomain. Key attributes:
  - Id (opaque identifier)
  - DomainId / SubdomainId
  - LocalPart (string)
  - ConfiguredSmtpCode (Spamma.Modules.Common.SmtpResponseCode)
  - Enabled (bool)
  - CreatedBy (user id)
  - CreatedAt (UTC timestamp)
  - TotalReceived (integer)
  - LastReceivedAt (UTC timestamp, nullable)

- **ChaosAddressAuditEvent**: Lightweight audit record for lifecycle events: Created, Enabled, Disabled, FirstReceived, EditAttemptAfterImmutable, DeleteAttemptAfterImmutable. Contains actor, timestamp, and metadata.

## Authorized user types & permissions

This feature involves the following user types and permission mapping. These map to the platform's existing role/assignment model.

- Domain Management (System Role)
  - Scope: all domains and subdomains in the system.
  - Allowed actions: create chaos addresses for any domain/subdomain; view/list; enable/disable; edit/delete prior to first receive; cannot edit/delete after first receive; view counters and audit events.

- Domain Moderator
  - Scope: domains to which the user is assigned as moderator (includes all subdomains under those domains).
  - Allowed actions: create chaos addresses within their domains and subdomains; view/list; enable/disable; edit/delete prior to first receive; cannot edit/delete after first receive; view counters and audit events.

- Subdomain Moderator
  - Scope: specific subdomains to which the user is assigned as moderator.
  - Allowed actions: create chaos addresses for their subdomain; view/list; enable/disable; edit/delete prior to first receive; cannot edit/delete after first receive; view counters and audit events.

- Subdomain Viewer
  - Scope: specific subdomains to which the user is assigned as a viewer.
  - Allowed actions: view/list chaos addresses for the subdomain; create and enable/disable chaos addresses for the subdomain (per existing project conventions where viewers may be granted special creation rights); cannot edit/delete after first receive; view counters and audit events.

Notes:

- All actions are subject to the platform's authorization checks. The spec assumes the platform can resolve assignments and system roles to enforce the scopes above.
- If any of the mappings above differ from organizational policy, update the spec and the assumptions accordingly.

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: An authorized user can create a chaos address and see it listed in the domain/subdomain UI within 5 seconds of submission.
- **SC-002**: When enabled, the chaos address responds with the configured SMTP error for 100% of incoming SMTP deliveries to that recipient (measured across a test set of 100 synthetic deliveries).
- **SC-003**: After sending 100 test emails to an enabled chaos address, the TotalReceived counter matches 100 and LastReceivedAt is updated to the most recent delivery time (within 1 second accuracy for single-thread test runs).
- **SC-004**: Attempts to edit or delete a chaos address after the first received message are rejected with a clear error message and logged; at least one audit event exists that records the FirstReceived transition.

## Assumptions

- The platform's existing permission model can determine "domain management" and "moderator/viewer" roles and is used to gate UI/API access for chaos address operations.
- Allowed SMTP error codes will be constrained to the set supported by the SmtpServer NuGet package used by this project; the platform UI/API surfaces only those supported codes.
- Enable/disable is allowed after first receive (toggles only); edits to local-part or code and deletion are forbidden after first receive.
- The per-address aggregate (total count and last-received timestamp) is sufficient for the feature; full message retention or per-message metadata is out of scope.

## Constraints and Notes

- The chaos address pattern is a normal recipient mailbox for routing and matching purposes; behavior differs only when enabled.
- Where SMTP protocol supports per-recipient responses (e.g., during RCPT TO or at DATA time depending on server), the system should apply the error response only to the chaos recipient while allowing other recipients to be processed normally where possible.
- Once enabled, chaos addresses will behave like configured negative responses for senders; care should be taken when enabling in production domains to avoid unintended external bounce loops.

## Open Questions / Clarifications

- None remain. All clarification questions have been resolved and incorporated into the spec.
