# BluQubeQuery Attribute - API Code Generation

## What is BluQubeQuery?

The `BluQubeQuery` attribute is a code generation decorator used in Blazor WebAssembly projects to automatically generate API endpoints for queries. When you decorate a query record with `[BluQubeQuery(Path = "...")]`, the framework generates the corresponding API endpoint.

## When to Use

**REQUIRED** for:
- Queries in `*.Client` projects (Blazor WebAssembly)
- Any query that needs to be accessed from WASM client code
- Enables automatic API endpoint code generation

**NOT required** for:
- Backend queries in main module projects
- Internal queries not exposed to WASM

## Pattern

```csharp
using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Query to retrieve authenticated user's passkeys.
/// </summary>
[BluQubeQuery(Path = "api/users/passkeys/my")]
public record GetMyPasskeysQuery : IQuery<GetMyPasskeysQueryResult>;
```

## Path Naming Convention

Follow the module's API path prefix:

| Module | Path Prefix | Example |
|--------|-------------|---------|
| UserManagement | `api/users/` | `api/users/passkeys/my` |
| DomainManagement | `api/domains/` | `api/domains/search` |
| EmailInbox | `email-inbox/` | `email-inbox/search-emails` |

## Examples from Codebase

**UserManagement queries**:
```csharp
[BluQubeQuery(Path = "api/users/stats")]
public record GetUserStatsQuery : IQuery<GetUserStatsQueryResult>;

[BluQubeQuery(Path = "api/users/search")]
public record SearchUsersQuery(...) : IQuery<SearchUsersQueryResult>;

[BluQubeQuery(Path = "api/users/passkeys/my")]
public record GetMyPasskeysQuery : IQuery<GetMyPasskeysQueryResult>;
```

**DomainManagement queries**:
```csharp
[BluQubeQuery(Path = "api/domains/search")]
public record SearchDomainsQuery(...) : IQuery<SearchDomainsQueryResult>;

[BluQubeQuery(Path = "api/subdomains/search")]
public record SearchSubdomainsQuery(...) : IQuery<SearchSubdomainsQueryResult>;
```

**EmailInbox queries**:
```csharp
[BluQubeQuery(Path = "email-inbox/search-emails")]
public record SearchEmailsQuery(...) : IQuery<SearchEmailsQueryResult>;
```

## Passkey Queries - Updated

All passkey queries now include the BluQubeQuery attribute:

✅ `GetMyPasskeysQuery` → `api/users/passkeys/my`  
✅ `GetUserPasskeysQuery` → `api/users/passkeys`  
✅ `GetPasskeyDetailsQuery` → `api/users/passkeys/details`  

## Key Takeaway

**Always decorate client project queries with `[BluQubeQuery(Path = "...")]`**

The path tells the code generator where to expose the query as an API endpoint, enabling seamless client-server communication in Blazor WebAssembly! 🚀
