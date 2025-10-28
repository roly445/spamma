"""
PASSKEY IMPLEMENTATION - CODE COMPLETION EXAMPLES

This file provides code examples for completing the critical TODO items
in the passkey implementation.
"""

# ============================================================================
# 1. GETTING CURRENT AUTHENTICATED USER CONTEXT
# ============================================================================

# PATTERN: Inject IHttpContextAccessor into your handler/processor
# Example from the codebase (check existing handlers for confirmation):

from typing import Optional
import uuid

class PasskeyCommandHandlerWithContext:
    """
    Example of how to retrieve current user from HttpContext
    """
    
    def __init__(self, http_context_accessor, passkey_repository):
        self._http_context_accessor = http_context_accessor
        self._passkey_repository = passkey_repository
    
    def get_current_user_id(self) -> uuid.UUID:
        """Extract current user ID from HttpContext claims"""
        http_context = self._http_context_accessor.http_context
        if http_context is None:
            raise ValueError("No HTTP context available")
        
        # Get user from HttpContext.User
        user = http_context.user
        if user is None or not user.identity.is_authenticated:
            raise ValueError("User not authenticated")
        
        # Extract user ID from claims (typically "sub" or "NameIdentifier")
        user_id_claim = None
        for claim in user.claims:
            if claim.type == "sub" or claim.type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":
                user_id_claim = claim.value
                break
        
        if user_id_claim is None:
            raise ValueError("User ID claim not found")
        
        return uuid.UUID(user_id_claim)


# C# EXAMPLE (for Spamma codebase):
"""
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Spamma.Modules.UserManagement.Application.CommandHandlers;

internal class RegisterPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    IHttpContextAccessor httpContextAccessor,  // <- Inject this
    TimeProvider timeProvider,
    IEnumerable<IValidator<RegisterPasskeyCommand>> validators,
    ILogger<RegisterPasskeyCommandHandler> logger) 
    : CommandHandler<RegisterPasskeyCommand>(validators, logger)
{
    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found or invalid");
        }
        
        return userId;
    }

    protected override async Task<CommandResult> HandleInternal(
        RegisterPasskeyCommand request, 
        CancellationToken cancellationToken)
    {
        // REPLACE: var currentUserId = Guid.Empty;
        // WITH:
        var currentUserId = this.GetCurrentUserId();

        // ... rest of handler
    }
}
"""


# ============================================================================
# 2. JWT TOKEN GENERATION IN AUTHENTICATION
# ============================================================================

# Find existing token generation logic in CompleteAuthenticationCommand or similar
# Pattern: Extract the token generation into a helper method or service

"""
C# PATTERN:

1. Find where JWT tokens are currently issued (likely in CompleteAuthenticationCommandHandler)
2. Extract or reuse that logic in AuthenticateWithPasskeyCommandHandler

Example structure:

public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    
    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GenerateToken(Guid userId, string emailAddress, SystemRole role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, emailAddress),
                new Claim(ClaimTypes.Role, role.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// In AuthenticateWithPasskeyCommandHandler:
internal class AuthenticateWithPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    IUserRepository userRepository,  // <- Need this to get user details
    JwtTokenService jwtTokenService, // <- Inject token service
    TimeProvider timeProvider,
    IEnumerable<IValidator<AuthenticateWithPasskeyCommand>> validators,
    ILogger<AuthenticateWithPasskeyCommandHandler> logger)
    : CommandHandler<AuthenticateWithPasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AuthenticateWithPasskeyCommand request,
        CancellationToken cancellationToken)
    {
        var passkeyMaybe = await passkeyRepository.GetByCredentialIdAsync(
            request.CredentialId, 
            cancellationToken);
            
        if (passkeyMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(
                UserManagementErrorCodes.PasskeyNotFound, 
                "Passkey not found"));
        }

        var passkey = passkeyMaybe.Value;

        var result = passkey.RecordAuthentication(
            request.SignCount, 
            timeProvider.GetUtcNow().UtcDateTime);
            
        if (result.IsFailure)
        {
            return CommandResult.Failed(result.Error);
        }

        var saveResult = await passkeyRepository.SaveAsync(passkey, cancellationToken);
        if (!saveResult.IsSuccess)
        {
            return CommandResult.Failed(new BluQubeErrorData(
                CommonErrorCodes.SavingChangesFailed));
        }

        // GET USER DETAILS and GENERATE TOKEN
        var userMaybe = await userRepository.GetByIdAsync(passkey.UserId, cancellationToken);
        if (userMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(
                CommonErrorCodes.NotFound, 
                "User not found"));
        }

        var user = userMaybe.Value;
        var token = this._jwtTokenService.GenerateToken(
            user.Id, 
            user.EmailAddress, 
            user.SystemRole);

        // TODO: Return token in response
        // This depends on the CommandResult response structure
        return CommandResult.Succeeded();
    }
}
"""


# ============================================================================
# 3. AUTHORIZATION POLICIES FOR PASSKEY ACCESS
# ============================================================================

"""
C# PATTERN - Implement OwnedPasskeyAuthorizer:

using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Spamma.Modules.UserManagement.Application.AuthorizationRequirements;

[BluQubeAuthorizer]
internal class OwnedPasskeyAuthorizer : IAuthorizer<GetPasskeyDetailsQuery>, IAuthorizer<RevokePasskeyCommand>
{
    private readonly IPasskeyRepository _passkeyRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OwnedPasskeyAuthorizer> _logger;

    public OwnedPasskeyAuthorizer(
        IPasskeyRepository passkeyRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OwnedPasskeyAuthorizer> logger)
    {
        _passkeyRepository = passkeyRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        GetPasskeyDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        var userRole = this.GetCurrentUserRole();

        // Admins can access any passkey
        if (userRole == SystemRole.UserManagement)
        {
            return AuthorizationResult.Success();
        }

        // Non-admins can only access their own passkeys
        var passkeyMaybe = await _passkeyRepository.GetByIdAsync(request.PasskeyId, cancellationToken);
        if (passkeyMaybe.HasNoValue)
        {
            return AuthorizationResult.Failed("Passkey not found");
        }

        if (passkeyMaybe.Value.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access passkey {PasskeyId} they don't own",
                userId,
                request.PasskeyId);
            return AuthorizationResult.Failed("Access denied");
        }

        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        RevokePasskeyCommand request,
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        var userRole = this.GetCurrentUserRole();

        // Admins can revoke any passkey
        if (userRole == SystemRole.UserManagement)
        {
            return AuthorizationResult.Success();
        }

        // Non-admins can only revoke their own passkeys
        var passkeyMaybe = await _passkeyRepository.GetByIdAsync(request.PasskeyId, cancellationToken);
        if (passkeyMaybe.HasNoValue)
        {
            return AuthorizationResult.Failed("Passkey not found");
        }

        if (passkeyMaybe.Value.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to revoke passkey {PasskeyId} they don't own",
                userId,
                request.PasskeyId);
            return AuthorizationResult.Failed("Access denied");
        }

        return AuthorizationResult.Success();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID claim not found or invalid");
        }

        return userId;
    }

    private SystemRole GetCurrentUserRole()
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Role)?.Value;

        if (Enum.TryParse<SystemRole>(roleClaim, out var role))
        {
            return role;
        }

        return SystemRole.User; // Default to regular user
    }
}

// Also for admin-only queries:

[BluQubeAuthorizer]
internal class UserManagementAdminAuthorizer : IAuthorizer<GetUserPasskeysQuery>, IAuthorizer<RevokeUserPasskeyCommand>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserManagementAdminAuthorizer> _logger;

    public UserManagementAdminAuthorizer(
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserManagementAdminAuthorizer> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        GetUserPasskeysQuery request,
        CancellationToken cancellationToken)
    {
        var userRole = this.GetCurrentUserRole();

        if (userRole != SystemRole.UserManagement)
        {
            var userId = this.GetCurrentUserId();
            _logger.LogWarning(
                "User {UserId} with role {Role} attempted to access passkeys for user {TargetUserId}",
                userId,
                userRole,
                request.UserId);
            return Task.FromResult(AuthorizationResult.Failed("Admin access required"));
        }

        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        RevokeUserPasskeyCommand request,
        CancellationToken cancellationToken)
    {
        var userRole = this.GetCurrentUserRole();

        if (userRole != SystemRole.UserManagement)
        {
            var userId = this.GetCurrentUserId();
            _logger.LogWarning(
                "User {UserId} with role {Role} attempted to revoke passkey for user {TargetUserId}",
                userId,
                userRole,
                request.UserId);
            return Task.FromResult(AuthorizationResult.Failed("Admin access required"));
        }

        return Task.FromResult(AuthorizationResult.Success());
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private SystemRole GetCurrentUserRole()
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<SystemRole>(roleClaim, out var role) ? role : SystemRole.User;
    }
}
"""


# ============================================================================
# 4. WEBAUTHN VERIFICATION ON BACKEND
# ============================================================================

"""
SECURITY CRITICAL: Verify WebAuthn responses on the backend

Install NuGet package: WebAuthn.Net

Example verification in handler:

using WebAuthn.Net;

internal class RegisterPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    IWebAuthnService webAuthnService,  // <- Library or custom service
    TimeProvider timeProvider,
    IEnumerable<IValidator<RegisterPasskeyCommand>> validators,
    ILogger<RegisterPasskeyCommandHandler> logger)
    : CommandHandler<RegisterPasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        RegisterPasskeyCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = this.GetCurrentUserId();

        // VERIFY credential registration response from frontend
        // 1. Validate attestation object (device authenticity)
        // 2. Verify challenge matches session state
        // 3. Extract public key from attestation

        try
        {
            var verificationResult = await this._webAuthnService.VerifyRegistrationResponseAsync(
                request.AttestationObject,  // from navigator.credentials.create()
                request.ClientDataJSON,     // also from navigator.credentials.create()
                storedChallenge,            // from session/database
                expectedOrigin,             // your app's origin
                expectedRpId,               // your Relying Party ID
                cancellationToken);

            if (!verificationResult.IsSuccess)
            {
                return CommandResult.Failed(new BluQubeErrorData(
                    UserManagementErrorCodes.InvalidPasskeyRegistration,
                    "Passkey verification failed"));
            }

            // Extract verified public key and credential ID
            var credentialId = verificationResult.CredentialId;
            var publicKey = verificationResult.PublicKey;

            // Continue with registration...
            var result = Passkey.Register(
                currentUserId,
                credentialId,
                publicKey,
                verificationResult.SignCount,
                request.DisplayName,
                verificationResult.Algorithm,
                timeProvider.GetUtcNow().UtcDateTime);

            if (result.IsFailure)
            {
                return CommandResult.Failed(result.Error);
            }

            // ... save and return
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "WebAuthn verification failed");
            return CommandResult.Failed(new BluQubeErrorData(
                UserManagementErrorCodes.InvalidPasskeyRegistration,
                "Passkey verification failed"));
        }
    }
}

// Similarly for authentication:

internal class AuthenticateWithPasskeyCommandHandler(
    IPasskeyRepository passkeyRepository,
    IUserRepository userRepository,
    IWebAuthnService webAuthnService,
    JwtTokenService jwtTokenService,
    TimeProvider timeProvider,
    IEnumerable<IValidator<AuthenticateWithPasskeyCommand>> validators,
    ILogger<AuthenticateWithPasskeyCommandHandler> logger)
    : CommandHandler<AuthenticateWithPasskeyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AuthenticateWithPasskeyCommand request,
        CancellationToken cancellationToken)
    {
        // VERIFY assertion response from frontend
        // 1. Find passkey by credential ID
        // 2. Verify signature using stored public key
        // 3. Check challenge matches
        // 4. Verify sign count increased (prevents cloning)

        var passkeyMaybe = await this._passkeyRepository.GetByCredentialIdAsync(
            request.AssertionResponse.Id,
            cancellationToken);

        if (passkeyMaybe.HasNoValue)
        {
            return CommandResult.Failed(new BluQubeErrorData(
                UserManagementErrorCodes.PasskeyNotFound,
                "Passkey not found"));
        }

        var passkey = passkeyMaybe.Value;

        try
        {
            var verificationResult = await this._webAuthnService.VerifyAssertionResponseAsync(
                request.AssertionResponse,
                passkey.PublicKey,
                storedChallenge,
                expectedOrigin,
                expectedRpId,
                passkey.SignCount,  // Compare against stored sign count
                cancellationToken);

            if (!verificationResult.IsSuccess)
            {
                return CommandResult.Failed(new BluQubeErrorData(
                    CommonErrorCodes.NotFound,
                    "Authentication failed"));
            }

            // Update passkey with new sign count and last used timestamp
            var result = passkey.RecordAuthentication(
                verificationResult.NewSignCount,
                timeProvider.GetUtcNow().UtcDateTime);

            if (result.IsFailure)
            {
                return CommandResult.Failed(result.Error);
            }

            var saveResult = await this._passkeyRepository.SaveAsync(passkey, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return CommandResult.Failed(new BluQubeErrorData(
                    CommonErrorCodes.SavingChangesFailed));
            }

            // Issue JWT token
            var userMaybe = await this._userRepository.GetByIdAsync(passkey.UserId, cancellationToken);
            if (userMaybe.HasNoValue)
            {
                return CommandResult.Failed(new BluQubeErrorData(
                    CommonErrorCodes.NotFound,
                    "User not found"));
            }

            var user = userMaybe.Value;
            var token = this._jwtTokenService.GenerateToken(
                user.Id,
                user.EmailAddress,
                user.SystemRole);

            // Return token in response
            return CommandResult.Succeeded();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "WebAuthn assertion verification failed");
            return CommandResult.Failed(new BluQubeErrorData(
                CommonErrorCodes.NotFound,
                "Authentication failed"));
        }
    }
}
"""


# ============================================================================
# 5. FRONTEND WEBAUTHN UTILITIES
# ============================================================================

"""
TypeScript file: Assets/Scripts/webauthn-passkey.ts

import { arrayBufferToBase64, base64ToArrayBuffer } from './encoding-utils';

export interface PasskeyRegistrationOptions {
  displayName: string;
  userId: string;
  userName: string;
  challenge: string;
  timeout: number;
}

export interface PasskeyAuthenticationOptions {
  challenge: string;
  timeout: number;
}

export interface PasskeyCredential {
  credentialId: string; // base64
  publicKey: string;    // base64
  signCount: number;
  algorithm: string;
}

export async function registerPasskey(
  options: PasskeyRegistrationOptions
): Promise<PasskeyCredential> {
  if (!window.PublicKeyCredential) {
    throw new Error("WebAuthn is not supported in this browser");
  }

  const credentialCreationOptions: CredentialCreationOptions = {
    publicKey: {
      challenge: base64ToArrayBuffer(options.challenge),
      rp: {
        name: "Spamma",
        id: window.location.hostname,
      },
      user: {
        id: base64ToArrayBuffer(options.userId),
        name: options.userName,
        displayName: options.displayName,
      },
      pubKeyCredParams: [
        { alg: -7, type: "public-key" },  // ES256
        { alg: -257, type: "public-key" }, // RS256
      ],
      timeout: options.timeout || 60000,
      attestation: "direct",
      userVerification: "preferred",
    },
  };

  const credential = await navigator.credentials.create(
    credentialCreationOptions
  ) as PublicKeyCredential | null;

  if (!credential) {
    throw new Error("Passkey registration was cancelled");
  }

  const response = credential.response as AuthenticatorAttestationResponse;

  return {
    credentialId: arrayBufferToBase64(credential.id),
    publicKey: arrayBufferToBase64(response.attestationObject),
    signCount: 0, // Will be set by server
    algorithm: "ES256",
  };
}

export async function authenticateWithPasskey(
  options: PasskeyAuthenticationOptions
): Promise<{
  credentialId: string;
  clientDataJSON: string;
  authenticatorData: string;
  signature: string;
  signCount: number;
}> {
  if (!window.PublicKeyCredential) {
    throw new Error("WebAuthn is not supported in this browser");
  }

  const credentialRequestOptions: CredentialRequestOptions = {
    publicKey: {
      challenge: base64ToArrayBuffer(options.challenge),
      timeout: options.timeout || 60000,
      userVerification: "preferred",
    },
  };

  const assertion = await navigator.credentials.get(
    credentialRequestOptions
  ) as PublicKeyCredential | null;

  if (!assertion) {
    throw new Error("Passkey authentication was cancelled");
  }

  const response = assertion.response as AuthenticatorAssertionResponse;

  return {
    credentialId: arrayBufferToBase64(assertion.id),
    clientDataJSON: arrayBufferToBase64(response.clientDataJSON),
    authenticatorData: arrayBufferToBase64(response.authenticatorData),
    signature: arrayBufferToBase64(response.signature),
    signCount: 0, // Server will verify against stored value
  };
}

// Helper for encoding
export function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary);
}

export function base64ToArrayBuffer(base64: string): ArrayBuffer {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}
"""

---

These examples provide the core patterns needed to complete the passkey implementation.
Key points:
1. Inject IHttpContextAccessor to get current user context
2. Extract existing JWT token generation or create a TokenService
3. Implement authorizers that check user ownership or admin role
4. Use WebAuthn.Net (or similar) to verify attestation/assertion responses
5. Update handlers with actual verification logic

For full integration testing, you'll need to set up test WebAuthn credentials.
