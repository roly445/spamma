# Data Model: Developer First Push API

**Feature**: 001-email-push-api
**Date**: November 10, 2025
**Purpose**: Define the data structures, relationships, and validation rules for push integrations and email notifications

## Entities

### PushIntegration

**Purpose**: Represents a developer's subscription configuration for receiving email push notifications.

**Fields**:

- `Id`: Guid (primary key, unique identifier)
- `UserId`: Guid (foreign key to authenticated user)
- `SubdomainId`: Guid (foreign key to subdomain, must have viewer permission)
- `Name`: string (optional display name, max 100 chars)
- `FilterType`: enum { AllEmails, SingleEmail, Regex } (notification scope)
- `FilterValue`: string? (email address for SingleEmail, regex pattern for Regex, null for AllEmails)
- `IsActive`: bool (default true, can be deactivated)
- `CreatedAt`: DateTimeOffset
- `UpdatedAt`: DateTimeOffset

**Relationships**:

- Belongs to User (many-to-one)
- Belongs to Subdomain (many-to-one, with viewer permission check)
- No direct relationship to emails (filtered at runtime)

**Validation Rules**:

- `UserId` must exist and be authenticated
- `SubdomainId` must exist and user must have viewer role
- `FilterType` determines `FilterValue` requirements:
  - AllEmails: `FilterValue` must be null
  - SingleEmail: `FilterValue` must be valid email address
  - Regex: `FilterValue` must be valid regex pattern (max 500 chars, no ReDoS risks)
- `Name` optional but recommended for management

**State Transitions**:

- Created: Initial state, IsActive = true
- Updated: Filter criteria changed, UpdatedAt modified
- Deactivated: IsActive = false (soft delete)
- Deleted: Hard delete from event store

### EmailNotification

**Purpose**: Minimal payload sent in push notifications containing essential email metadata.

**Fields**:

- `EmailId`: Guid (references the full email content)
- `To`: string (recipient email address)
- `From`: string (sender email address)
- `Subject`: string (email subject line, truncated to 200 chars if longer)
- `ReceivedAt`: DateTimeOffset (when email was received)

**Relationships**:

- References EmailContent by EmailId
- No direct persistence (transient object for streaming)

**Validation Rules**:

- All fields required and non-empty
- `To` and `From` must be valid email addresses
- `Subject` truncated if > 200 chars
- `EmailId` must reference existing email content

### EmailContent

**Purpose**: Full MIME message content stored for retrieval via GET endpoint.

**Fields**:

- `Id`: Guid (primary key)
- `MimeMessage`: string (full MIME content as EML format)
- `SizeBytes`: long (size of MIME content)
- `StoredAt`: DateTimeOffset
- `SubdomainId`: Guid (subdomain that received the email)

**Relationships**:

- Belongs to Subdomain (many-to-one)
- Referenced by EmailNotification

**Validation Rules**:

- `MimeMessage` must be valid MIME format
- `SizeBytes` must match actual content size
- `SubdomainId` must exist
- Content must be retrievable within performance constraints

## Domain Rules

### Push Integration Filtering

- **AllEmails**: Matches any email received in the subdomain
- **SingleEmail**: Matches emails where recipient exactly matches FilterValue
- **Regex**: Matches emails where recipient matches the regex pattern in FilterValue

### Permission Enforcement

- Users can only create integrations for subdomains where they have viewer role
- Email content retrieval requires viewer permission for the email's subdomain
- JWT tokens must include user identity and validated claims

### Data Lifecycle

- Push integrations are event-sourced: CreatePushIntegration, UpdatePushIntegration, DeletePushIntegration events
- Email content stored indefinitely (no automatic deletion)
- Failed notifications do not prevent email storage

## Validation Constraints

### Regex Patterns

- Maximum length: 500 characters
- Timeout: 100ms execution limit
- Forbidden patterns: catastrophic backtracking risks
- Validation on creation/update

### Email Addresses

- Standard RFC 5322 format validation
- Maximum length: 254 characters
- Case-insensitive matching for filtering

### Content Size

- MIME content: Maximum 10MB per email
- Subject truncation: 200 characters with ellipsis