# SSL/TLS Certificate Management - Let's Encrypt Integration

## Overview

Spamma implements automated SSL/TLS certificate generation and renewal using Let's Encrypt and the ACME v2 protocol through the **Certes** library. Certificates are generated using HTTP-01 challenges, stored locally, and automatically renewed daily.

## Architecture

### Components

#### 1. **CertesLetsEncryptService** (EmailInbox.Infrastructure)
- **Purpose**: Let's Encrypt certificate generation using ACME v2 protocol
- **Location**: `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/CertesLetsEncryptService.cs`
- **Responsibilities**:
  - Create or retrieve ACME account with Let's Encrypt
  - Place new certificate orders for domain(s)
  - Extract HTTP-01 challenge tokens and key authorization
  - Register challenge responses via `IAcmeChallengeResponder`
  - Generate private keys (ES256)
  - Download and return PFX-format certificates
  - Clear challenges after completion
  - Handle errors with Result monad

**Key Methods**:
```csharp
public async Task<Result<byte[], string>> GenerateCertificateAsync(
    string domain,
    string email,
    bool useStaging,
    IAcmeChallengeResponder challengeResponder,
    CancellationToken cancellationToken)
```

**Returns**: PFX certificate bytes (password: `letmein`) or error message string

#### 2. **IAcmeChallengeResponder** (Common.Infrastructure.Contracts)
- **Purpose**: Abstraction for HTTP-01 challenge response handling
- **Location**: `src/modules/Spamma.Modules.Common/Infrastructure/Contracts/IAcmeChallengeResponder.cs`
- **Responsibility**: Interface that both stores and retrieves ACME challenge tokens

**Methods**:
```csharp
Task RegisterChallengeAsync(string token, string keyAuth, CancellationToken cancellationToken);
Task ClearChallengesAsync(CancellationToken cancellationToken);
```

#### 3. **AcmeChallengeServer** (Spamma.App.Infrastructure.Services)
- **Purpose**: HTTP server listening on port 80 to respond to ACME HTTP-01 challenges
- **Location**: `src/Spamma.App/Spamma.App/Infrastructure/Services/AcmeChallengeServer.cs`
- **Responsibilities**:
  - Thread-safe in-memory token storage (ConcurrentDictionary)
  - Implements `IAcmeChallengeResponder` interface
  - Stores challenge tokens and key authorizations
  - Provides lookup method for middleware
  - Clears all stored challenges after validation
  - Logs all challenge operations

**Key Methods**:
```csharp
public async Task RegisterChallengeAsync(string token, string keyAuth, CancellationToken cancellationToken)
public async Task ClearChallengesAsync(CancellationToken cancellationToken)
public string? GetChallenge(string token)
```

#### 4. **AcmeChallengeMiddleware** (Spamma.App.Infrastructure.Middleware)
- **Purpose**: HTTP middleware that intercepts ACME challenge requests
- **Location**: `src/Spamma.App/Spamma.App/Infrastructure/Middleware/AcmeChallengeMiddleware.cs`
- **Responsibilities**:
  - Intercepts requests to `/.well-known/acme-challenge/{token}`
  - Retrieves stored challenge via `AcmeChallengeServer`
  - Returns 200 + key authorization if found
  - Returns 404 if token not found
  - Passes through non-ACME requests to next middleware
  - Logs diagnostic information

**Integration**:
```csharp
// In Program.cs
app.UseAcmeChallenge();  // Registered via middleware
```

#### 5. **CertificateRenewalBackgroundService** (Spamma.App.Infrastructure.Services)
- **Purpose**: Daily certificate renewal background task
- **Location**: `src/Spamma.App/Spamma.App/Infrastructure/Services/CertificateRenewalBackgroundService.cs`
- **Schedule**: Runs daily at 2:00 AM UTC with 1-hour check interval
- **Responsibilities**:
  - Loads domain and email from `IAppConfigurationService`
  - Checks certificate expiration from `/app/certs/` storage
  - Triggers renewal if <30 days remaining
  - Stores renewed certificate to `/app/certs/`
  - Maintains rollback support (keeps last 3 certificates)
  - Deletes old certificates to prevent storage bloat
  - Logs renewal attempts and results

**Certificate Storage**:
- Location: `/app/certs/` (Docker volume or mounted host path)
- Naming: `{domain}_{timestamp}.pfx`
- Example: `example.com_2024-01-15_02-00-00.pfx`
- Retention: Last 3 certificates for rollback

#### 6. **GenerateCertificateEndpoint** (Spamma.App.Infrastructure.Endpoints.Setup)
- **Purpose**: REST API endpoint for certificate generation during setup
- **Location**: `src/Spamma.App/Spamma.App/Infrastructure/Endpoints/Setup/GenerateCertificateEndpoint.cs`
- **Route**: `POST /api/setup/generate-certificate`
- **Responsibilities**:
  - Accept domain, email, and staging flag from setup UI
  - Call `CertesLetsEncryptService.GenerateCertificateAsync()`
  - Handle validation errors (domain/email required)
  - Return success/failure response with certificate metadata
  - Handle ACME-level errors (Let's Encrypt service)

**Request**:
```csharp
public record GenerateCertificateRequest
{
    public required string Domain { get; init; }
    public required string Email { get; init; }
    public bool UseStaging { get; init; } = false;
}
```

**Response**:
```csharp
public record GenerateCertificateResponse
{
    public required bool Success { get; init; }
    public required string Domain { get; init; }
    public required string CertificatePath { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required string Message { get; init; }
}
```

#### 7. **Certificates.razor** (Spamma.App.Components.Pages.Setup)
- **Purpose**: UI component for certificate configuration during setup
- **Location**: `src/Spamma.App/Spamma.App/Components/Pages/Setup/Certificates.razor`
- **Position in Setup**: Step 4 (Email → Certificates → Admin → Complete)
- **Features**:
  - **Option 1 - Skip for Now**: Skip certificate setup, proceed to next step
  - **Option 2 - Let's Encrypt**: 
    - Domain input field with validation
    - Email input field with validation (must contain @)
    - Staging environment checkbox
    - Generate button with progress indicator
    - Error message display
    - Success message after generation
  - **Option 3 - Manual Upload**: Placeholder for future implementation

**Validation**:
- Domain: Required, non-empty
- Email: Required, non-empty, must contain @

**API Integration**:
- Calls `POST /api/setup/generate-certificate`
- Uses `HttpClient` for WASM context
- Displays progress while generating
- Shows success/error messages

## HTTP-01 Challenge Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User initiates certificate generation                         │
│    POST /api/setup/generate-certificate                          │
│    { "domain": "example.com", "email": "admin@example.com", ... }
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. GenerateCertificateEndpoint                                   │
│    Calls CertesLetsEncryptService.GenerateCertificateAsync()    │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. CertesLetsEncryptService                                      │
│    a) Create/retrieve ACME account (email)                      │
│    b) Place order for domain                                    │
│    c) Extract HTTP-01 challenge (token + keyAuth)              │
│    d) Call challengeResponder.RegisterChallengeAsync()          │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. AcmeChallengeServer.RegisterChallengeAsync()                 │
│    Stores token + keyAuth in ConcurrentDictionary              │
│    Token now accessible at memory for HTTP lookups             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. CertesLetsEncryptService                                      │
│    Calls httpChallenge.Validate()                               │
│    Let's Encrypt initiates HTTP-01 validation                  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Let's Encrypt Validation                                      │
│    Makes HTTP request:                                          │
│    GET /.well-known/acme-challenge/{token} (port 80)           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. AcmeChallengeMiddleware (port 80 listener)                   │
│    a) Intercepts request to /.well-known/acme-challenge/{token}│
│    b) Retrieves keyAuth from AcmeChallengeServer                │
│    c) Returns 200 + keyAuth body                                │
│    d) Let's Encrypt verifies keyAuth matches                   │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 8. CertesLetsEncryptService                                      │
│    a) Challenge validated by Let's Encrypt                      │
│    b) Generate private key (ES256)                              │
│    c) Create certificate signing request                        │
│    d) Download certificate from Let's Encrypt                  │
│    e) Convert to PFX format                                     │
│    f) Call challengeResponder.ClearChallengesAsync()           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 9. AcmeChallengeServer.ClearChallengesAsync()                   │
│    Clears all tokens from memory                                │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 10. Certificate returned                                         │
│     GenerateCertificateEndpoint returns 200 + response          │
│     { "success": true, "domain": "example.com", ... }           │
└─────────────────────────────────────────────────────────────────┘
```

## Dependency Injection Configuration

### EmailInbox Module
```csharp
// EmailInbox/Module.cs
services.AddScoped<ICertesLetsEncryptService, CertesLetsEncryptService>();
```

### Spamma.App Program.cs
```csharp
// Register services
services.AddSingleton<AcmeChallengeServer>();
services.AddSingleton<IAcmeChallengeResponder>(sp => sp.GetRequiredService<AcmeChallengeServer>());
services.AddHostedService<CertificateRenewalBackgroundService>();

// Register middleware
app.UseAcmeChallenge();
```

## Port 80 Requirement

**Critical**: HTTP-01 challenges require port 80 to be accessible from the internet on the domain being validated.

**Setup Requirements**:
1. **Docker**: Expose port 80 (default in docker-compose.yml)
2. **Port Forwarding**: Router must forward port 80 → application host
3. **Firewall**: No firewall blocking port 80 inbound
4. **DNS**: Domain must resolve to public IP where Spamma is running
5. **Staging**: Use Let's Encrypt staging during testing (limited rate limits)

**Error if Port 80 Unavailable**:
```
Certificate generation failed: Unable to connect to /.well-known/acme-challenge/...
Connection timeout or refused
```

## Staging vs Production

### Staging Environment
- **URL**: `https://acme-staging-v02.api.letsencrypt.org/directory`
- **Use Case**: Testing certificate generation without hitting rate limits
- **Certificate Chain**: Self-signed, untrusted by browsers
- **Rate Limits**: Generous (1000/week per domain)
- **Flag**: `useStaging: true`

### Production Environment
- **URL**: `https://acme-v02.api.letsencrypt.org/directory`
- **Use Case**: Real certificates for live deployment
- **Certificate Chain**: Trusted by all modern browsers
- **Rate Limits**: Strict (50/week per registered domain)
- **Flag**: `useStaging: false`

## Certificate Storage and Renewal

### Storage Location
- **Docker**: `/app/certs/` volume mount
- **File Format**: PFX (encrypted with password: `letmein`)
- **Naming Pattern**: `{domain}_{timestamp}.pfx`

### Renewal Schedule
- **Trigger**: Runs daily at 2:00 AM UTC
- **Check Interval**: Checks every hour if renewal time reached
- **Renewal Threshold**: Renews if <30 days until expiration
- **Backup**: Keeps last 3 certificates for rollback

### Renewal Logic
1. Calculate next renewal time (expires_at - 30 days)
2. If now >= next_renewal_time:
   a. Generate new certificate
   b. Store to `/app/certs/{domain}_{timestamp}.pfx`
   c. Delete oldest certificate if >3 exist
   d. Log renewal result

## Configuration

### Environment-Specific Settings

Certificate renewal behavior can be controlled via `appsettings.json`:

```json
{
  "Certificates": {
    "UseStagingServer": false
  }
}
```

**Settings**:
- **`Certificates:UseStagingServer`** (boolean, default: `false`)
  - `false` (Production): Uses Let's Encrypt production servers (`letsencrypt.org`)
  - `true` (Development): Uses Let's Encrypt staging servers (`staging.letsencrypt.org`)
  - **Important**: Staging certificates are not trusted by browsers, intended for testing only

### Development Configuration

For local development and testing, enable Let's Encrypt staging:

**appsettings.Development.json**:
```json
{
  "Certificates": {
    "UseStagingServer": true
  }
}
```

**Benefits**:
- No rate limits (production has strict limits to prevent abuse)
- Certificates auto-issued immediately for testing
- Perfect for testing certificate generation and renewal flows
- No impact on production deployments (staging only during development)

### Production Configuration

For production deployments, either:
1. Omit the `Certificates` section entirely (defaults to production)
2. Explicitly set `"UseStagingServer": false`

```json
{
  "Certificates": {
    "UseStagingServer": false
  }
}
```

**Note**: Production certificates are trusted by all browsers and are included in the global CA store.

### How Renewal Service Uses Configuration

The `CertificateRenewalBackgroundService` reads this setting at runtime:

```csharp
var useStaging = this._configuration.GetValue<bool>("Certificates:UseStagingServer", defaultValue: false);
if (useStaging)
{
    this._logger.LogWarning("Using Let's Encrypt STAGING server for certificate renewal (for testing only)");
}

var result = await certService.GenerateCertificateAsync(
    domain, email, useStaging: useStaging, challengeResponder, cancellationToken);
```

- Daily renewal checks configuration at runtime
- No restart required to change settings
- Logging provides visibility when staging mode is active
- Default (production) used if setting missing

## Error Handling

All certificate operations use the **Result<T, TError>** monad for error handling:

```csharp
public async Task<Result<byte[], string>> GenerateCertificateAsync(...)
```

**Common Errors**:
- `"Domain is required"` - Empty or whitespace domain
- `"Email is required"` - Empty or whitespace email
- `"Certificate generation failed: {reason}"` - ACME protocol error
- `"Unable to connect to port 80"` - Port 80 not accessible
- `"Domain validation timeout"` - Let's Encrypt couldn't validate challenge

## Testing

### Unit Tests
- **Location**: `tests/Spamma.Modules.EmailInbox.Tests/Infrastructure/Services/`
- **File**: `CertesLetsEncryptServiceTests.cs`
- **Coverage**: Input validation, error cases, challenge responder integration
- **Count**: 11 tests

### Integration Tests
- **Location**: `tests/Spamma.App.Tests/Infrastructure/`
- **Files**:
  - `Services/AcmeChallengeServerTests.cs` (10 tests)
  - `Endpoints/GenerateCertificateEndpointTests.cs` (14 tests)
  - `Middleware/AcmeChallengeMiddlewareTests.cs` (6 tests)
- **Total**: 30+ tests

**Run Tests**:
```powershell
dotnet test tests/Spamma.Modules.EmailInbox.Tests/
dotnet test tests/Spamma.App.Tests/
```

## Production Deployment

### Prerequisites
1. **Domain Registration**: Must own the domain being validated
2. **DNS Setup**: Domain must resolve to Spamma's public IP
3. **Port 80**: Must be accessible from internet
4. **Email**: Valid email for Let's Encrypt account (for expiry notices)
5. **Storage**: `/app/certs/` must be persistent volume

### Steps
1. Navigate to setup wizard: `https://your-domain/setup/certificates`
2. Enter domain name and email
3. Ensure `Use Staging` is **unchecked** (if testing, check it first)
4. Click "Generate Certificate"
5. Wait for certificate generation (typically 30-60 seconds)
6. Success message indicates certificate ready at `/app/certs/`

### HTTPS Configuration
After certificate generation, configure ASP.NET Core to use the generated certificate:

```csharp
// In Program.cs
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(443, configure => configure
        .UseHttps("/app/certs/example.com_2024-01-15_02-00-00.pfx", "letmein"));
    options.ListenAnyIP(80); // For HTTP-01 challenges
});
```

## Troubleshooting

### Certificate Generation Fails
**Check**:
1. Port 80 is accessible: `nc -zv your-domain 80`
2. DNS resolves correctly: `nslookup your-domain`
3. Let's Encrypt isn't rate-limited: Check `WellKnownServers` URL in service
4. Domain/email inputs are valid (non-empty, email contains @)

### Certificate Renewal Fails
**Check**:
1. Log files for `CertificateRenewalBackgroundService` errors
2. Verify `/app/certs/` directory exists and is writable
3. Confirm port 80 still accessible (renewal uses HTTP-01 again)
4. Check disk space for certificate storage

### Domain Validation Timeout
**Cause**: Let's Encrypt can't reach `/.well-known/acme-challenge/{token}`

**Fix**:
1. Verify port 80 forwarding: `curl http://your-domain/.well-known/acme-challenge/test`
2. Check firewall rules: No rules blocking port 80
3. Verify DNS: `nslookup your-domain` returns correct IP
4. Check middleware order: Ensure `app.UseAcmeChallenge()` comes before routing

## Configuration Files

- **Spamma.App/Spamma.App.csproj**: Certes package reference
- **Spamma.Modules.EmailInbox/Spamma.Modules.EmailInbox.csproj**: Certes package reference
- **Spamma.App/Program.cs**: DI registration and middleware setup
- **Spamma.Modules.EmailInbox/Module.cs**: Service registration
- **AssemblyInfo.cs**: InternalsVisibleTo attributes for testing

## Related Files

- `src/modules/Spamma.Modules.Common/Infrastructure/Contracts/IAcmeChallengeResponder.cs`
- `src/modules/Spamma.Modules.EmailInbox/Infrastructure/Services/CertesLetsEncryptService.cs`
- `src/Spamma.App/Spamma.App/Infrastructure/Services/AcmeChallengeServer.cs`
- `src/Spamma.App/Spamma.App/Infrastructure/Middleware/AcmeChallengeMiddleware.cs`
- `src/Spamma.App/Spamma.App/Infrastructure/Services/CertificateRenewalBackgroundService.cs`
- `src/Spamma.App/Spamma.App/Infrastructure/Endpoints/Setup/GenerateCertificateEndpoint.cs`
- `src/Spamma.App/Spamma.App/Components/Pages/Setup/Certificates.razor`

## References

- **ACME v2 Protocol**: https://datatracker.ietf.org/doc/html/rfc8555
- **Let's Encrypt Documentation**: https://letsencrypt.org/docs/
- **HTTP-01 Challenge**: https://letsencrypt.org/docs/challenge-types/#http-01
- **Certes Library**: https://github.com/fszlin/certes
- **PFX Certificate Format**: https://en.wikipedia.org/wiki/PKCS_12

