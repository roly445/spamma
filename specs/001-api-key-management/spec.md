# Feature Specification: API Key Management

**Feature Branch**: `001-api-key-management`  
**Created**: November 10, 2025  
**Status**: Draft  
**Input**: User description: "API Key management I want to swap the JWT authentication for a simple API key mechanism. I want users to be able to create these keys, with a name and be able to revoke them via the UI. Keys will be viewable once its created and at no time after that. As the public-facing API is lightweight, consisting of a grpc endpoint and a download of the mime message, there is no reason to have it with granular permissions"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create API Keys (Priority: P1)

As an authenticated user, I want to create API keys with custom names so that I can use them to authenticate API requests.

**Why this priority**: This is the core functionality needed to enable API key authentication.

**Independent Test**: Can be fully tested by creating an API key and verifying it appears in the user's key list with the correct name.

**Acceptance Scenarios**:

1. **Given** user is authenticated, **When** user provides a name and requests key creation, **Then** system generates a unique API key and displays it once
2. **Given** user attempts to create a key with an empty name, **When** user submits the form, **Then** system shows validation error
3. **Given** user creates multiple keys, **When** user views their keys, **Then** each key has a unique name and creation timestamp

---

### User Story 2 - Revoke API Keys (Priority: P1)

As an authenticated user, I want to revoke API keys via the UI so that I can disable access for compromised or unused keys.

**Why this priority**: This is essential for security management of API keys.

**Independent Test**: Can be fully tested by revoking a key and verifying it no longer works for API authentication.

**Acceptance Scenarios**:

1. **Given** user has active API keys, **When** user revokes a key, **Then** the key becomes inactive and cannot be used for authentication
2. **Given** user revokes a key, **When** system receives API request with revoked key, **Then** system returns authentication error
3. **Given** user revokes a key, **When** user views their keys, **Then** revoked key shows as inactive with revocation timestamp

---

### User Story 3 - View API Keys (Priority: P2)

As an authenticated user, I want to see my API keys and their status so that I can manage them effectively.

**Why this priority**: This supports the key management workflow but is secondary to creation and revocation.

**Independent Test**: Can be fully tested by viewing the key list and verifying status information is displayed correctly.

**Acceptance Scenarios**:

1. **Given** user has created API keys, **When** user views their keys, **Then** system shows key names, creation dates, and status (active/revoked)
2. **Given** user has revoked a key, **When** user views their keys, **Then** revoked keys show revocation timestamp
3. **Given** user views a newly created key, **When** key is displayed, **Then** the full key value is not shown (only metadata)

---

### User Story 4 - API Key Authentication (Priority: P1)

As a system, I want to authenticate API requests using API keys instead of JWT tokens so that the authentication mechanism is simplified for the lightweight public API.

**Why this priority**: This is the core change replacing JWT authentication.

**Independent Test**: Can be fully tested by making API requests with valid API keys and verifying successful authentication.

**Acceptance Scenarios**:

1. **Given** valid API key is provided, **When** API request is made to gRPC endpoint, **Then** system authenticates successfully
2. **Given** valid API key is provided, **When** MIME message download is requested, **Then** system authenticates successfully
3. **Given** invalid or revoked API key is provided, **When** API request is made, **Then** system returns authentication failure

### Edge Cases

- What happens when user tries to revoke an already revoked key?
- How does system handle concurrent API requests with the same key during revocation?
- What happens if user creates multiple keys with the same name?
- How does system handle API key validation for high-volume requests?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to create API keys with custom names
- **FR-003**: System MUST display the full API key value to the user immediately after creation, but not thereafter
- **FR-004**: System MUST allow authenticated users to revoke API keys via the UI
- **FR-005**: System MUST prevent revoked API keys from being used for authentication
- **FR-006**: System MUST authenticate API requests using API keys instead of JWT tokens for public endpoints
- **FR-007**: System MUST support existing public API functionality (gRPC endpoint and MIME message download) with API key authentication
- **FR-008**: System MUST show API key metadata (name, creation date, status) in the user interface
- **FR-009**: System MUST validate API key names are not empty and between 1-100 characters in length
- **FR-010**: System MUST provide clear, actionable error messages with specific error codes (format: API_KEY_*) for API key authentication failures
- **FR-011**: API key names MUST be unique within each user's collection

### Code Quality & Project Structure (MANDATORY for PRs)

- **CQ-001**: All code MUST compile with zero warnings. Any new warnings must be addressed before merging.
- **CQ-002**: Public types MUST use clear intent naming. XML documentation comments (`///`) are NOT required and MUST NOT be used for public API intent — code clarity and naming SHOULD provide intent.
- **CQ-003**: Blazor components MUST be split into a `.razor` file and a `.razor.cs` code-behind file for interactive logic; server-side-only static pages are exempt when explicitly marked.
- **CQ-004**: Commands and Queries types MUST be declared in the respective `.Client` project. Their handlers, validators and authorizers MUST be implemented in the non-`.Client` server project.
- **CQ-005**: No new project (csproj) is to be added to the repository without explicit approval from maintainers (documented in the PR).

### Key Entities *(include if feature involves data)*

- **API Key**: Represents an authentication token with a user-defined name, unique key value, creation timestamp, and revocation status
- **User**: Authenticated user who owns and manages API keys

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create API keys with custom names in under 30 seconds from the UI
- **SC-002**: Users can revoke API keys in under 10 seconds from the UI
- **SC-003**: API key authentication works for 100% of valid requests to public endpoints
- **SC-004**: Revoked API keys are rejected for 100% of authentication attempts
- **SC-005**: System maintains 99.9% uptime for API key validation under 1000 concurrent requests

## Clarifications

### Session 2025-11-10

- Q: API Key Security Posture → A: Standard security - Secure generation + HTTPS requirement for all API calls
- Q: API Key Validation Scalability → A: Medium load - Up to 1000 concurrent requests
- Q: Error Handling for API Key Failures → A: User-friendly messages with error codes
- Q: API Key Name Uniqueness → A: Unique per user - Names must be unique within each user's key collection
- Q: API Key Observability Requirements → A: Key usage logs and metrics

## Non-Functional Requirements

- **NFR-001**: API keys MUST be generated using SHA-256 hashing with per-key salt for cryptographic security and uniqueness
- **NFR-002**: All API calls MUST use HTTPS to protect key transmission
- **NFR-003**: System MUST validate API keys with <100ms p95 response time under normal load
- **NFR-004**: System MUST support up to 1000 concurrent API key validation requests
- **NFR-005**: System MUST log API key usage attempts and provide metrics for authentication success/failure rates
