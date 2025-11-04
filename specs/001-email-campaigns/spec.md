```markdown
# Feature Specification: Email campaigns

**Feature Branch**: `001-email-campaigns`  
**Created**: 2025-11-04  
**Status**: Draft  
**Input**: User description: "Email campaigns - this is a feature that enables users of spamma to send bulk emails to a subdomain in spamma without saving the content of the emails just recording the email was received. We might want to save one email per campaign so the user can check the content. A campaign is not created in the ui but is created on the fly by the presence of a custom header in the email. The system will dictate what the header is, but the value will be down to the user sending the email campaigns. There needs to be a page on the app where users can see the results of a campaign, this would be a paged list with the subdomain, campaign value, first and last date of the emails captured, and the total captured. The user can only see the subdomains they have access to. From there, is a drill into page where the user can see a timing graph of how the emails came in and the sample email that was saved. From the inbox, an email will be visually tagged with being part of a campaign and when the click to view the email, the email viewer will have a cta to the campaign page for that email."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View campaign list (Priority: P1)

As an authenticated user with access to one or more subdomains, I want to see a paged list of campaigns so I can quickly understand which campaigns have received emails for my subdomains.

**Why this priority**: Provides the primary visibility into campaign activity and answers the core user need: did my campaign(s) receive messages?

**Independent Test**: Given a user with access to subdomain A, when several emails are received containing the campaign header for values X and Y, then the campaign list shows two campaign rows scoped to subdomain A with correct counts and date ranges.

**Acceptance Scenarios**:

1. **Given** user has access to subdomain `example.spamma.io` and campaigns `promo-1` and `promo-2` exist, **When** the user visits the Campaigns page, **Then** they see a paged list with rows containing: Subdomain, Campaign value, First received date, Last received date, Total captured.
2. **Given** there are more campaigns than fit on a page, **When** the user navigates pages, **Then** the list provides correct pagination and preserves sorting/filtering state.
3. **Given** a user has access only to subdomain B, **When** they view campaigns, **Then** they see only campaigns for subdomain B and cannot see campaigns for other subdomains.

---

### User Story 2 - Campaign drill-in & sample (Priority: P1)

As a user, I want to drill into a campaign row to see a timing graph and a sample saved email so I can inspect the campaign's cadence and at least one message content.

**Why this priority**: Enables operational troubleshooting and verification of campaign content and timing.

**Independent Test**: Given a campaign has recorded events, when the user clicks into the campaign, they see a timeline chart of emails by time bucket and a single saved sample email.

**Acceptance Scenarios**:

1. **Given** a campaign has captured N emails, **When** the user opens the campaign detail, **Then** they see a timing graph (e.g., counts per minute/hour/day depending on range) and a single sample email display with subject, sender, received timestamp and a CTA to open the email in the inbox viewer.
2. **Given** no sample email was saved for a campaign, **When** the user opens the detail, **Then** the UI shows an informative placeholder explaining no sample was captured and an option to enable sample capture for future messages (if allowed by policy).

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

- What happens when an email contains multiple campaign headers? [NEEDS CLARIFICATION: choose priority parsing rule — first header wins, last header wins, or ignore multi-header messages]
- What happens when campaign header values collide across different subdomains? (Expect campaigns scoped per subdomain; same header value on different subdomains are distinct campaigns.)
- Handling large burst traffic: timing graph should gracefully degrade (aggregate into larger buckets) instead of plotting every message point.
- GDPR / PII: campaigns should default to NOT storing full message bodies; only a single sample may be stored per campaign (configurable and opt-in).
- Invalid header values (maliciously large or non-printable): header values must be sanitized and limited to an allowed length (e.g., 255 chars).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST recognize a campaign when an incoming email contains a configured campaign header (header name determined by system configuration) and extract the campaign value.
- **FR-002**: System MUST record the email as part of the campaign without persisting the full message body by default; the system MUST increment the campaign count and update first/last received timestamps.
- **FR-003**: System MUST allow saving a single sample email per campaign; when the first email for a campaign is eligible and sample capture is enabled, the system stores that message's metadata and content for display purposes only.
- **FR-004**: System MUST expose a Campaigns page listing campaigns scoped to subdomains the current user has access to, with paging, sorting (by last received, total captured), and basic filtering by campaign value and date range.
- **FR-005**: System MUST enforce access control: users can only view campaigns for subdomains they have permission for.
- **FR-006**: System MUST provide a campaign detail view that renders a timing graph of message counts over time and displays the saved sample email (if present).
- **FR-007**: System MUST visually tag inbox messages that belong to a campaign and include a CTA in the message viewer linking to that campaign's detail page.
- **FR-008**: System MUST sanitize and validate campaign header values (max length, printable characters) and reject or truncate values that exceed limits.
- **FR-009**: System MUST provide a configuration setting (operator-controlled) to enable or disable saving sample emails per campaign and to control sample retention period.
- **FR-010**: System MUST not persist message bodies for campaign-tracked emails unless explicitly enabled by configuration; audit logs must record when samples are stored.

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

## Open Questions [NEEDS CLARIFICATION]

1. [Q1] Multi-header handling: If an email contains multiple campaign headers, should the system: A) use the first header value, B) use the last header value, or C) treat the message as ineligible and ignore campaign tracking?  
   - Suggested default: A) use the first header value.  

2. [Q2] Sample capture policy: Should sample capture be off by default and operator-controlled (recommended), or should it be user-opt-in per campaign?  
   - Suggested default: Operator-controlled, off by default; opt-in enables storing a single sample per campaign.  

3. [Q3] Header length limit: What is the maximum allowed length for campaign header values?  
   - Suggested default: 255 characters (sanitized and truncated).

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
