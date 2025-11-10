# Research: API Key Management

**Feature**: API Key Management
**Date**: November 10, 2025
**Research Tasks**:

- API key authentication patterns in ASP.NET Core
- Secure storage mechanisms for API keys
- Migration strategy from JWT to API key authentication
- UI/UX patterns for API key management
- Performance optimization for API key validation

## Findings & Decisions

### API Key Authentication Patterns

**Decision**: Use ASP.NET Core's built-in authentication middleware with custom API key handler

**Rationale**: ASP.NET Core provides extensible authentication framework. Custom API key authentication handler can integrate seamlessly with existing authorization policies and middleware pipeline.

**Alternatives Considered**:

- Custom middleware only - Rejected due to lack of integration with ASP.NET Core auth framework
- Third-party libraries - Rejected to maintain control and avoid additional dependencies

**Implementation Approach**:

- Create `ApiKeyAuthenticationHandler` inheriting from `AuthenticationHandler<ApiKeyAuthenticationOptions>`
- Validate API keys against hashed values in database
- Support both header-based (`X-API-Key`) and query parameter authentication
- Integrate with existing authorization policies

### Secure API Key Storage

**Decision**: Store API keys as SHA-256 hashes with salt

**Rationale**: Hashing prevents recovery of original keys if database is compromised, while allowing validation. Salting prevents rainbow table attacks.

**Alternatives Considered**:

- Plain text storage - Rejected due to security risks
- Encryption with reversible keys - Rejected as overkill for validation-only use case
- BCrypt/PBKDF2 - Rejected due to performance impact on validation (SHA-256 is faster for high-throughput scenarios)

**Implementation Details**:

- Generate cryptographically secure random keys (32+ bytes)
- Use SHA-256 with per-key salt
- Store hash and salt in database
- Validate by re-hashing input and comparing

### Migration from JWT to API Key Authentication

**Decision**: Dual authentication support during transition, then remove JWT

**Rationale**: Allows gradual migration without breaking existing functionality. Public API endpoints will support both auth methods initially.

**Alternatives Considered**:

- Immediate cutover - Rejected due to risk of breaking existing integrations
- API versioning - Rejected as overkill for authentication method change

**Migration Steps**:

1. Add API key authentication alongside JWT
2. Update documentation and client examples
3. Monitor usage and deprecate JWT after grace period
4. Remove JWT authentication code

### UI Patterns for API Key Management

**Decision**: Dedicated API keys page with create/revoke actions and metadata display

**Rationale**: Clear separation of concerns from other user settings. One-time key display prevents accidental exposure.

**Alternatives Considered**:

- Inline in user profile - Rejected due to clutter
- Modal dialogs only - Rejected for complex workflows
- Third-party key management UI - Rejected to maintain consistency

**UI Components**:

- List view showing key name, creation date, status
- Create button opening form with name input
- Revoke buttons with confirmation
- Success message showing key value once

### Performance Optimization for API Key Validation

**Decision**: In-memory caching with database fallback

**Rationale**: API key validation needs to be fast for high-throughput scenarios. Caching reduces database load while maintaining consistency.

**Alternatives Considered**:

- Database-only validation - Rejected for performance under load
- Distributed cache (Redis) - Rejected as overkill for current scale requirements

**Implementation**:

- Cache valid API keys in memory with short TTL
- Background refresh from database
- Immediate invalidation on revocation
- Fallback to database for cache misses

## Open Questions Resolved

All research tasks completed. No remaining unknowns for implementation planning.