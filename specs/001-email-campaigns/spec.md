```markdown
# Feature Specification: Email campaigns

**Feature Branch**: `001-email-campaigns`  
**Created**: 2025-11-04  
**Status**: Draft  
**Input**: User description: "Email campaigns - this is a feature that enables users of spamma to send bulk emails to a subdomain in spamma without saving the content of the emails just recording the email was received. We might want to save one email per campaign so the user can check the content. A campaign is not created in the ui but is created on the fly by the presence of a custom header in the email. The system will dictate what the header is, but the value will be down to the user sending the email campaigns. There needs to be a page on the app where users can see the results of a campaign, this would be a paged list with the subdomain, campaign value, first and last date of the emails captured, and the total captured. The user can only see the subdomains they have access to. From there, is a drill into page where the user can see a timing graph of how the emails came in and the sample email that was saved. From the inbox, an email will be visually tagged with being part of a campaign and when the click to view the email, the email viewer will have a cta to the campaign page for that email."

## Clarifications

### Session 2025-11-04

- Q1: Sample retention → 30 days (default). Samples may be deleted manually before the retention period by operators or authorized users. An export mechanism is requested (see next clarification question).  

 - Export formats: both CSV and JSON will be supported (CSV for analytics/BI, JSON for structured export including sample preview).  
 - Q2: Export formats → both CSV and JSON.
 - Q3: Export permissions → any user with access to the subdomain may request exports (subject to existing access controls and operator policies).
 - Q4: Export delivery → synchronous immediate browser download (requested). Large exports may be subject to size limits; operators can configure server-side limits and fallback behavior.

- Q5: Campaign value case-sensitivity → Case-insensitive: campaign values will be normalized to lower-case for storage and querying. Display may preserve original-case for UI if desired, but all matching and indexing MUST use the normalized lower-case form.
 - Q6: Default synchronous export limit → Default synchronous export size/time limit: 10,000 rows. Operators can configure this threshold per deployment.

- Q7: Capture timing → Asynchronous (recommended): campaign captures will be recorded asynchronously after the SMTP response is returned to the sender. The system MUST record a lightweight receipt/audit event synchronously at ingress, retry failed persistence attempts, and surface failures in audit logs and operator dashboards.


## User Scenarios & Testing *(mandatory)*

### User Story 1 - View campaign list (Priority: P1)

As an authenticated user with access to one or more subdomains, I want to see a paged list of campaigns so I can quickly understand which campaigns have received emails for my subdomains.

**Why this priority**: Provides the primary visibility into campaign activity and answers the core user need: did my campaign(s) receive messages?

**Independent Test**: Given a user with access to subdomain A, when several emails are received containing the campaign header for values X and Y, then the campaign list shows two campaign rows scoped to subdomain A with correct counts and date ranges.

**Acceptance Scenarios**:

1. **Given** user has access to subdomain `example.spamma.io` and campaigns `promo-1` and `promo-2` exist, **When** the user visits the Campaigns page, **Then** they see a paged list with rows containing: Subdomain, Campaign value, First received date, Last received date, Total captured.
2. **Given** there are more campaigns than fit on a page, **When** the user navigates pages, **Then** the list provides correct pagination and preserves sorting/filtering state.
3. **Given** a user has access only to subdomain B, **When** they view campaigns, **Then** they see only campaigns for subdomain B and cannot see campaigns for other subdomains.
4. **Given** a user is viewing the Campaigns page, **When** they click the manual "Refresh" control, **Then** the UI re-queries the backend and displays updated counts and last-received timestamps.

---

### User Story 2 - Campaign drill-in & sample (Priority: P1)

As a user, I want to drill into a campaign row to see a timing graph and a sample saved email so I can inspect the campaign's cadence and at least one message content.

**Why this priority**: Enables operational troubleshooting and verification of campaign content and timing.

**Independent Test**: Given a campaign has recorded events, when the user clicks into the campaign, they see a timeline chart of emails by time bucket and a single saved sample email.

**Acceptance Scenarios**:

1. **Given** a campaign has captured N emails, **When** the user opens the campaign detail, **Then** they see a timing graph (e.g., counts per minute/hour/day depending on range) and a single sample email display with subject, sender, received timestamp and a CTA to open the email in the inbox viewer.
2. **Given** no sample email was saved for a campaign, **When** the user opens the detail, **Then** the UI shows an informative placeholder explaining no sample was captured and an option to enable sample capture for future messages (if allowed by policy).
3. **Given** an incoming campaign email was addressed to a chaos address for that subdomain, **When** the email is processed by the SMTP ingress, **Then** the system records the campaign capture (count and timestamps) and the SMTP session returns the configured chaos-address response (for example, MailboxNameNotAllowed) so the sender is informed the mailbox is not deliverable.

  Note: Recording is best-effort and MAY be persisted asynchronously; the system MUST audit the receipt, retry persistence on transient failures, and surface capture failures to operators. Returning the SMTP chaos response MUST NOT be suppressed by capture attempts.

---

### User Story 3 - Inbox tagging & journey (Priority: P2)

As a user browsing the inbox for a subdomain, I want emails that are part of a campaign to be visually tagged and to have a CTA in the email viewer linking me to the campaign page.

**Why this priority**: Improves discoverability of campaign context from individual messages and connects inbox experience to campaign analytics.

**Independent Test**: Given an inbox message was received with the campaign header, when viewing the message list it shows a campaign badge; when opening the email, the message viewer shows a CTA that navigates to the campaign detail page.

**Acceptance Scenarios**:

1. **Given** an email contains the campaign header, **When** the inbox list renders, **Then** the email row shows a campaign badge with the campaign value.
2. **Given** an email contains the campaign header, **When** viewing the message, **Then** the viewer shows a CTA labelled "View campaign" linking to the campaign's drill-in page and, if the sample saved is this message, indicates that this is the captured sample.

---

### Edge Cases

- What happens when an email contains multiple campaign headers? (Resolved: first header value is used.)
- What happens when campaign header values collide across different subdomains? (Campaigns are scoped per subdomain; same header value on different subdomains are distinct campaigns.)
- Handling large burst traffic: timing graph should gracefully degrade (aggregate into larger buckets) instead of plotting every message point.
- GDPR / PII: the system stores a single sample email per campaign by default (for display) but does not persist full message bodies beyond that sample unless explicitly changed by operator configuration. Sample storage is subject to retention policy and auditing.
- Invalid header values (maliciously large or non-printable): header values must be sanitized and limited to an allowed length (resolved: 255 chars max, truncated/sanitized).

- Campaign-tagged messages exclusion: Campaign-tagged messages are excluded from the standard inbox auto-deletion policy to ensure campaign analytics remain accurate; retention and deletion of saved samples remain governed by sample retention settings and the explicit delete API (FR-011).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST recognize a campaign when an incoming email contains a configured campaign header (header name determined by system configuration) and extract the campaign value.
- **FR-002**: System MUST record the email as part of the campaign without persisting the full message body by default; the system MUST increment the campaign count and update first/last received timestamps.
 - **FR-002**: System MUST record the email as part of the campaign without persisting the full message body by default; the system MUST increment the campaign count and update first/last received timestamps.
 - **FR-003**: System MUST store a single sample email per campaign by default. When the first email for a campaign arrives, the system shall record that first message's metadata and a truncated/sanitized content preview for display purposes. The sample MUST be the first captured message and MUST NOT be replaced by later messages unless an operator explicitly reconfigures the policy. The sample is visible to users who have access to the subdomain's inbox.
 - **FR-004**: System MUST expose a Campaigns page listing campaigns scoped to subdomains the current user has access to, with paging, sorting (by last received, total captured), and basic filtering by campaign value and date range.
- **FR-005**: System MUST enforce access control: users can only view campaigns for subdomains they have permission for.
- **FR-006**: System MUST provide a campaign detail view that renders a timing graph of message counts over time and displays the saved sample email (if present).
- **FR-007**: System MUST visually tag inbox messages that belong to a campaign and include a CTA in the message viewer linking to that campaign's detail page.
- **FR-008**: System MUST sanitize and validate campaign header values (max length, printable characters) and reject or truncate values that exceed limits.
- **FR-009**: System MUST provide operator-controlled configuration settings for campaign sample retention period and maximum stored preview length; operators MAY be able to disable sample storage, but the default behavior is to store a single sample per campaign.
 - **FR-010**: System MUST not persist full message bodies for campaign-tracked emails except for the single sample per campaign (stored as a truncated/sanitized preview); audit logs must record when samples are stored and why.
 - **FR-011**: System MUST provide an explicit delete API (operator and authorized-user action) to remove stored campaign samples prior to retention expiry; deletion actions MUST be auditable. The delete API MUST be protected by role-based access control; by default only users with the Admin role (represented by server-side authorization requirement types such as `CanModerationChaosAddressesRequirement` / `MustBeAdminRequirement`) may perform campaign-wide deletes. Delete operations MUST record an audit event containing the actor, timestamp, scope (subdomain + campaign value), and deletion mode (soft/hard).

 - **FR-017**: Capture persistence timing: Campaign capture records MAY be persisted asynchronously after the SMTP response is returned. The system MUST:
   - persist a lightweight receipt/audit event at ingress time (synchronously) to prove receipt;
  - ensure a short synchronous acknowledgement is written and visible to the capture pipeline such that at least 95% of captures are recorded to the read-model within 5 seconds under normal load (see SC-002);
   - retry persistence of campaign aggregates and samples on transient failures;
   - surface persistent failures in audit logs and operator dashboards for remediation;
   - allow operators to configure a hybrid mode (short synchronous timeout with async fallback).
- **FR-012**: System MUST provide an export capability allowing authorized users to export campaign data (CSV/JSON) that includes campaign metadata (subdomain, campaign value, first/last timestamps, total captured) and the sample preview where present; exports must respect access control and privacy configuration (samples exported only if sample storage is enabled and the requester has appropriate permissions).

 - **FR-013**: Export authorization: The system MUST permit export requests from any user who has read access to the target subdomain's inbox and campaigns. Operators MAY additionally restrict export rights via role-based configuration.
 - **FR-014**: Export auditing and rate-limiting: All export requests and completed exports MUST be logged (who requested, what was exported, timestamp). The system MUST support rate-limiting or throttling for export requests to prevent large-scale data exfiltration.

- **FR-015**: Export delivery: The system MUST support synchronous immediate downloads for exports under configured size/time limits. If an export exceeds those limits, the system MUST return a clear error and suggest alternatives (reduce scope or request an async/export job). Operators must be able to configure synchronous limits.
  - For larger exports the system MUST support an async export job lifecycle (JobId, status endpoint, IO store delivery and notification). Operators must be able to configure synchronous limits and fallback behavior.
 - **FR-015**: Export delivery: The system MUST support synchronous immediate downloads for exports under configured size/time limits. The default synchronous row-limit MUST be 10,000 rows; operators MAY configure a different threshold (rows or time). If an export exceeds those limits, the system MUST return a clear error and suggest alternatives (reduce scope or request an async/export job). Operators must be able to configure synchronous limits and fallback behavior.

- **FR-016**: Auto-delete exclusion: The system MUST exclude any message identified as belonging to a campaign from the standard inbox auto-deletion process. Campaign-tagged messages MUST remain discoverable in campaign counts and in the inbox until explicitly deleted via the normal user or admin delete actions or via the campaign-sample delete API (FR-011). This exclusion applies regardless of global auto-delete schedules.

 - **FR-018**: Manual refresh control: The system MUST provide a manual "Refresh" control on the Campaigns list and campaign detail pages that triggers a re-query of campaign counts and metadata. The UI MUST display updated counts within 5 seconds of the backend responding to the query. This control allows users to opt-in to refreshing read-models that are populated asynchronously.

**Acceptance scenario (export)**:

1. **Given** a user with export permissions for subdomain `example.spamma.io`, **When** they request an export for campaign `promo-1` in CSV format, **Then** the system returns a CSV file containing rows with campaign metadata and a column for sample preview (if present) and logs the export action.
2. **Given** the same user requests JSON, **When** the export completes, **Then** a structured JSON payload is returned containing campaign metadata and (if present) a sanitized sample preview nested within the campaign record.
3. **Given** the system has an auto-delete job configured to remove standard inbox messages older than X days, **When** the auto-delete executes, **Then** any messages marked as campaign hits are NOT removed by that job and remain visible in the inbox and campaign counts.
 - **FR-011**: If an incoming campaign email is addressed to a chaos address for the targeted subdomain, the system MUST both: (a) record the campaign capture (increment counts, update timestamps, and optionally save sample if enabled) and (b) return the configured SMTP response for chaos-address recipients to the sender (for example, a MailboxNameNotAllowed response). The system MUST ensure that recording the campaign does not silently suppress the SMTP error and that both outcomes are auditable.
 - Chaos address interaction: when a campaign email is sent to a chaos address the system will both register the campaign hit and return the chaos-address SMTP response. Tests should verify ordering (capture occurs and is visible in the campaign counts) and that senders receive the expected SMTP error code.

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero warnings. Any new warnings must be addressed before merging.
- **CQ-002**: Public types MUST use clear intent naming. XML documentation comments (`///`) are NOT required and MUST NOT be used for public API intent — code clarity and naming SHOULD provide intent.
- **CQ-003**: Blazor components MUST be split into a `.razor` file and a `.razor.cs` code-behind file for interactive logic; server-side-only static pages are exempt when explicitly marked.
- **CQ-004**: Commands and Queries types MUST be declared in the respective `.Client` project. Their handlers, validators and authorizers MUST be implemented in the non-`.Client` server project.
- **CQ-005**: No new project (csproj) is to be added to the repository without explicit approval from maintainers (documented in the PR).

### Key Entities *(include if feature involves data)*

- **CampaignSummary**: Represents an aggregated campaign record for a subdomain.
  - Attributes: SubdomainId, CampaignValue, FirstReceivedAt, LastReceivedAt, TotalCaptured
- **CampaignSample**: Optional stored sample for a campaign.
  - Attributes: CampaignId, MessageId, Subject, From, To, ReceivedAt, StoredAt, ContentPreview (sanitized/truncated)
- **CampaignCaptureConfig**: Operator-managed configuration controlling header name, sample capture enabled, sample retention period, max header length.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Campaign list view loads and renders the first page within 1.5s for a user with up to 1000 campaigns.
- **SC-002**: At least 95% of campaign-related emails are tracked (count incremented) within 5 seconds of receipt in normal load conditions.
- **SC-003**: Users only see campaigns for subdomains they have access to — verified by access-control tests (no cross-subdomain leakage in 100% of test runs).
- **SC-004**: Sample storage policy honored: by default, message bodies are not persisted; when sample capture is enabled, a single saved sample per campaign is retrievable and displays subject/from/timestamp and a truncated content preview.
- **SC-005**: Timing graph displays aggregated counts that accurately reflect stored counts (±0%) for buckets shown.

### Non-functional Notes

- The implementation must scale to handle bursts where campaigns receive thousands of messages in minutes. The timing graph should aggregate into larger buckets to keep UI performance acceptable.
- Security: Campaign header values and saved samples must be sanitized to prevent XSS or injection in the UI.
- Privacy: Respect PII policies — do not persist message bodies unless explicitly enabled; provide operator controls and audit logging for sample storage.

## Assumptions

- Campaigns are scoped to a subdomain; identical campaign header values on different subdomains are treated as separate campaigns.
- Campaign header name will be globally configured by operators; users supply the header value.
- The system's default is NOT to store email bodies; storing a single sample per campaign is opt-in via configuration.
- The inbox and campaign views share the same authorization model present in the system (users have explicit access lists for subdomains).

## Clarifications resolved

- Q1 (multi-header handling): Option A — use the first header value.
- Q2 (sample capture policy): System stores one sample per campaign by default; samples are visible to users who can view emails for that subdomain. Operators may configure retention and may disable sample storage if necessary.
- Q3 (header length limit): 255 characters maximum; values are sanitized and truncated to this limit.
 - Q5 (case-sensitivity): Campaign header values are case-insensitive; values are normalized to lower-case for storage and queries (matching is performed against the normalized form).
 - Q7 (capture timing): Asynchronous (recommended) — campaign captures are recorded asynchronously after SMTP response; receipt events are audited and retries are performed on failure.
 - Q8 (counts freshness): Manual refresh — the UI will provide a manual "Refresh" control to re-query campaign counts; counts are eventually consistent and updated via asynchronous persistence.
 - Q9 (sample-selection): First message — the sample saved for a campaign is the first message seen for that campaign and is not replaced by subsequent messages unless the operator changes policy.

---

## Files to create / edit (implementation notes)

- UI: Add Campaigns page and Campaign detail page under `Spamma.App.Client` with `.razor` + `.razor.cs` files.
- API: Add query `GetCampaignsQuery` in `Spamma.Modules.DomainManagement.Client` and corresponding query processor in `Spamma.Modules.DomainManagement`.
- Capture: Update SMTP/email ingestion service in `Spamma.Modules.EmailInbox` to detect campaign header and record captures via `ICommander` (`RecordCampaignCaptureCommand`).
- Storage: Add a CampaignReadModel/Projection in `Spamma.Modules.EmailInbox/Infrastructure/Projections` to maintain `CampaignSummary` documents.
- Config: Add `CampaignCaptureConfig` operator-managed settings in `Spamma.Modules.Common.Settings` or module-specific settings.

---

**Spec ready for planning** — next steps: /speckit.plan to create tasks and estimates.

```
# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

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

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero warnings. Any new warnings must be addressed before merging.
- **CQ-002**: Public types MUST use clear intent naming. XML documentation comments (`///`) are NOT required and MUST NOT be used for public API intent — code clarity and naming SHOULD provide intent.
- **CQ-003**: Blazor components MUST be split into a `.razor` file and a `.razor.cs` code-behind file for interactive logic; server-side-only static pages are exempt when explicitly marked.
- **CQ-004**: Commands and Queries types MUST be declared in the respective `.Client` project. Their handlers, validators and authorizers MUST be implemented in the non-`.Client` server project.
- **CQ-005**: No new project (csproj) is to be added to the repository without explicit approval from maintainers (documented in the PR).

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
