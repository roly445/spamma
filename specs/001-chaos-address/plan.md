# Implementation Plan: Chaos Monkey Email Address (001-chaos-address)

This plan breaks the spec into implementation tasks, mapping to modules, files, tests, and estimates. It follows the project's Clean Architecture and CQRS patterns.

Summary

- Feature branch: `001-chaos-address`

- Goal: Allow authorized users to create "chaos addresses" that, when enabled, cause the SMTP server to respond with a configured error code for that recipient. Track total received and last-received timestamp. Addresses are immutable (no edits/deletes) after first receive; enable/disable allowed after first receive.

Implementation phases (prioritized)

## Domain model & projections (Spamma.Modules.DomainManagement)

### Tasks — Domain model & projections

- Add `ChaosAddress` domain aggregate or entity under DomainManagement.Domain (if domain aggregate already exists, add a child entity).

- Add projections/read models: `ChaosAddressLookup` with fields: Id, DomainId/SubdomainId (required), LocalPart, ConfiguredSmtpCode (SmtpResponseCode enum), Enabled, CreatedBy, CreatedAt, TotalReceived, LastReceivedAt.

- Register projections in Module configuration.

### Files (Domain model)

- src/modules/Spamma.Modules.DomainManagement/Domain/ChaosAddress/ChaosAddress.cs

- src/modules/Spamma.Modules.DomainManagement/Infrastructure/Projections/ChaosAddressLookupProjection.cs

- src/modules/Spamma.Modules.DomainManagement/Infrastructure/ReadModels/ChaosAddressLookup.cs

### Tests (Domain model)

- Unit tests for aggregate behavior (creation, enable/disable, first-receive transition to immutable)

### Estimate (Domain model)

- 1.5 - 2 days

## Commands, queries & handlers (DomainManagement module + Client contracts)

### Tasks — Commands, queries & handlers

- Add commands: `CreateChaosAddressCommand`, `EnableChaosAddressCommand`, `DisableChaosAddressCommand`, `EditChaosAddressCommand` (pre-first-receive only), `DeleteChaosAddressCommand` (pre-first-receive only).

- Add queries: `GetChaosAddressesQuery`, `GetChaosAddressQuery` (for UI listing/detail).

- Add handlers with validation (FluentValidation) ensuring permission checks and immutability rules.

- Add Client-side query & command DTOs in the `.Client` project with `BluQubeCommand` / `BluQubeQuery` attributes for WASM codegen.

### Files (Commands/Client)

- src/modules/Spamma.Modules.DomainManagement/Application/Commands/CreateChaosAddress/CreateChaosAddressCommand.cs

- src/modules/Spamma.Modules.DomainManagement/Application/CommandHandlers/CreateChaosAddressCommandHandler.cs

- src/modules/Spamma.Modules.DomainManagement.Client/Application/Commands/CreateChaosAddressCommand.cs

- src/modules/Spamma.Modules.DomainManagement.Client/Application/Queries/GetChaosAddressesQuery.cs

### Tests (Commands/Handlers)

- Handler tests with mocked repository and publisher; verify events and behavior including rejection on edits/deletes after first receive.

### Estimate (Commands/Handlers)

- 2 - 3 days

### EmailInbox processing change (Spamma.Modules.EmailInbox)

- Tasks:

  - On inbound SMTP delivery path (SpammaMessageStore / message router), detect recipients and match them in delivery order against configured enabled `ChaosAddress` entries for the target domain/subdomain.

  - Apply the chaos rule to the first recipient that matches a configured enabled `ChaosAddress`: immediately respond for that recipient with the configured SMTP error using the SmtpServer package semantics (per-recipient response where possible). Update the ChaosAddress aggregate/readmodel with TotalReceived++ and LastReceivedAt atomically.

  - If no recipient in the message matches any enabled ChaosAddress, the message MUST flow through the normal inbound processing pipeline (no special response).

  - If a chaos address is disabled, fall back to normal processing for that recipient.

  - Ensure behavior for messages with multiple recipients: chaos behavior applies only to the first matching recipient; other recipients MUST be processed normally where the SMTP protocol and SmtpServer library allow per-recipient responses. Document any server-level fallback behavior if the library does not support per-recipient responses in the chosen handler path.

- Files (suggested):

  - src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/SpammaMessageStore.cs (or where recipient handling lives)

  - src/modules/Spamma.Modules.EmailInbox/Application/Commands/ReceivedChaosAddressNotificationCommand.cs (optional: or reuse ReceivedEmailCommand semantics)

- Tests:

  - Unit tests that simulate SaveAsync on message with chaos recipient (mock repository/querier and message store provider) verifying SmtpResponse returned and counters updated.

  - Integration tests with the real SmtpServer (or test double) asserting proper SMTP response codes are returned to a sender.

- Estimate: 2 - 3 days (integration tests may add time)

### UI & API (Spamma.App.Client / DomainManagement admin pages)

- Tasks:

  - Add UI to create chaos address (local-part, select supported SMTP code from dropdown, optional description); show status (enabled/disabled), counters, last received timestamp, and immutability state.

  - Add enable/disable controls and prevent edit/delete when immutable.

  - Add listing in SubdomainDetails page (there is already a placeholder referencing "Chaos Addresses").

- Files (suggested):

  - src/Spamma.App/Spamma.App.Client/Pages/Admin/SubdomainDetails.razor (augment existing UI)

  - src/Spamma.App/Spamma.App.Client/Components/ChaosAddress/ChaosAddressList.razor

  - src/Spamma.App/Spamma.App.Client/Components/ChaosAddress/CreateChaosAddress.razor

- Tests:

  - Basic component tests (if present) or manual QA scenarios.

- Estimate: 2 - 3 days

### Tests & integration

- Unit tests: aggregate, handlers, email inbox message store logic (happy path + error paths)

- Integration tests: end-to-end SMTP send to chaos address with both enabled and disabled states; verify counting and immutability.

- Estimate: 1.5 - 2.5 days

### Docs & release notes

- Add README or docs describing feature, supported SMTP codes (SmtpServer limitations), and operational cautions (avoid enabling in production without care).

- Add changelog entry.

- Estimate: 0.5 days

### Finalize, code review & merge

- Run full build, lint, and test suites. Address any StyleCop or Sonar rules flagged.

- Prepare PR description referencing `specs/001-chaos-address/spec.md` and checklist.

- Estimate: 0.5 - 1 day

Total estimate (rough): 10.5 - 15.5 developer days (depending on integration testing scope and parallelization).

Acceptance tests (high-level)

- Create chaos address as Domain Management user: verify stored as disabled and visible in UI.

- Enable chaos address: send SMTP message to that recipient → sending client receives configured SMTP error; TotalReceived increments; LastReceivedAt updated.

- Disable chaos address: send SMTP message → message processed normally.

- First-receive immutability: after one receive, attempt to edit/delete → operations rejected; enable/disable still allowed.

- Multi-recipient semantics: message to chaos recipient and normal recipient behaves as documented; specifically, chaos behavior is applied to the first matching recipient only and other recipients are processed normally where supported.

Risks and notes

- Ensure SmtpServer supports per-recipient responses in the flow used by the project; if not, document fallback behavior.

- Audit logging and immutability enforcement must be consistent and atomic (transactions) to avoid race conditions.

- Styling/UX: warn users in UI when enabling a chaos address on production domains.

Next steps I can take (choose one or more):
- Scaffold domain aggregate + projection + initial unit tests (I will create files and tests under DomainManagement module).
- Implement EmailInbox recipient handling and unit tests for message store behaviour.
- Scaffold API/Client DTOs and minimal UI stubs for creation/listing.
