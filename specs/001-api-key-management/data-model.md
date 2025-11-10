# Data Model: API Key Management

**Feature**: API Key Management
**Date**: November 10, 2025

## Entities

### API Key

Represents an authentication token for API access.

**Fields**:

- `Id`: Guid (primary key, unique identifier)
- `UserId`: Guid (foreign key to User, required)
- `Name`: string (user-defined name, 1-100 characters, required)
- `KeyHash`: string (SHA-256 hash of the API key, required)
- `Salt`: string (salt used for hashing, required)
- `CreatedAt`: DateTimeOffset (creation timestamp, required)
- `RevokedAt`: DateTimeOffset? (revocation timestamp, nullable)
- `IsRevoked`: bool (computed from RevokedAt presence)

**Validation Rules**:

- Name: Not null or empty, 1-100 characters, unique within user's keys
- KeyHash: Valid SHA-256 hash format
- Salt: Non-empty string
- CreatedAt: Not in future
- RevokedAt: If present, must be >= CreatedAt

**Relationships**:

- Belongs to User (many-to-one)
- No other direct relationships

**State Transitions**:

- Created (Active): Initial state after creation
- Revoked: Terminal state, cannot be undone

### User

Existing entity representing authenticated users.

**Additional Fields**: None required for this feature

**Relationships**:

- Has many API Keys (one-to-many)

## Domain Logic

### API Key Creation

- Generate cryptographically secure random key (32 bytes)
- Generate unique salt (16 bytes)
- Compute SHA-256 hash of (key + salt)
- Validate name uniqueness within user scope
- Create API Key entity with all fields
- Return both entity and plain key value (for one-time display)

### API Key Validation

- Retrieve API key by hash match
- Check if not revoked
- Verify user association if needed
- Return validation result

### API Key Revocation

- Find API key by ID and user ownership
- Set RevokedAt to current timestamp
- Update IsRevoked computed field
- Invalidate any cached entries

## Data Access Patterns

### Queries

- Get API keys by user (for listing)
- Get API key by hash (for validation)
- Check name uniqueness within user scope

### Commands

- Create API key
- Revoke API key

## Event Sourcing

All API key operations should be event-sourced for auditability:

- `ApiKeyCreated` event
- `ApiKeyRevoked` event

Events stored in Marten for the User aggregate or dedicated API Key aggregate.

## Security Considerations

- Key values never stored in plain text
- Hashes prevent key recovery
- Salts prevent rainbow table attacks
- Audit trail via events
- User isolation prevents key enumeration across users