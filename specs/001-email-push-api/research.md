# Research: Developer First Push API

**Feature**: 001-email-push-api
**Date**: November 10, 2025
**Purpose**: Resolve technical unknowns and establish best practices for gRPC-based push notifications in .NET 9

## Research Tasks

### 1. gRPC Streaming Implementation in .NET 9

**Task**: Research best practices for implementing server-streaming gRPC services in ASP.NET Core for real-time push notifications.

**Findings**:

- Use `Grpc.AspNetCore` package (latest version compatible with .NET 9)
- Implement server-streaming RPC where client sends subscription request and server streams email notifications
- Handle connection lifecycle: client connects with JWT, server validates and starts streaming
- Use `IServerStreamWriter<EmailNotification>` for streaming responses
- Implement proper error handling and reconnection logic

**Decision**: Use gRPC server-streaming with JWT authentication in request headers

**Rationale**: Provides real-time push capability with standard .NET tooling and good performance

**Alternatives Considered**: WebSockets (more complex for HTTP/2), Server-Sent Events (simpler but less efficient for bidirectional)

### 2. JWT Authentication in gRPC Services

**Task**: Research JWT token validation in gRPC service methods.

**Findings**:

- gRPC services can access HTTP context via `ServerCallContext`
- Extract JWT from `Authorization` header
- Use `JwtBearer` middleware or manual validation with `JwtSecurityTokenHandler`
- Validate token and extract claims for user identification and permissions

**Decision**: Manual JWT validation in gRPC service using `JwtSecurityTokenHandler`

**Rationale**: Allows fine-grained control over authentication in streaming context

**Alternatives Considered**: ASP.NET Core authentication middleware (less suitable for gRPC streaming)

### 3. Marten Projections for Push Integrations

**Task**: Research Marten projection patterns for managing push integration state.

**Findings**:

- Use `Marten` event store for push integration lifecycle events
- Create projection to maintain current state of active integrations
- Query projections for filtering emails to subscribed integrations
- Handle concurrent updates with optimistic concurrency

**Decision**: Single projection table for active push integrations with subdomain and filter criteria

**Rationale**: Efficient querying and state management aligned with existing Marten patterns

**Alternatives Considered**: In-memory caching (not persistent), separate database table (redundant with event store)

### 4. Regex Performance and Security

**Task**: Research safe regex usage for email filtering in high-volume scenarios.

**Findings**:

- Use `Regex.Match()` with timeout to prevent ReDoS attacks
- Validate regex patterns on creation to reject malicious patterns
- Consider compiled regex for performance if patterns are reused
- Limit regex complexity based on pattern length and features used

**Decision**: Validate and compile regex patterns with timeout protection

**Rationale**: Balances flexibility with security and performance

**Alternatives Considered**: No regex support (less flexible), unrestricted regex (security risk)

### 5. gRPC Error Handling and Connection Management

**Task**: Research error handling patterns for gRPC streaming connections.

**Findings**:

- Use gRPC status codes for different error types (UNAUTHENTICATED, PERMISSION_DENIED, etc.)
- Handle client disconnections gracefully
- Implement retry logic on client side
- Log connection events without exposing sensitive data

**Decision**: Standard gRPC error codes with custom error details for domain-specific errors

**Rationale**: Follows gRPC best practices and provides clear error communication

**Alternatives Considered**: HTTP status codes (not applicable to gRPC), custom error format (less standard)