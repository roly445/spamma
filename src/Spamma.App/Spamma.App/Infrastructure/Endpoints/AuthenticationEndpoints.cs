using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Spamma.App.Infrastructure.Contracts;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Infrastructure.Endpoints;

internal static class AuthenticationEndpoints
{
    internal static void MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("api/auth/assertion-options", GetAssertionOptions)
            .WithName("GetAssertionOptions")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapPost("api/auth/make-assertion", MakeAssertion)
            .WithName("MakeAssertion")
            .Produces(StatusCodes.Status200OK);
    }

    private static IResult GetAssertionOptions(HttpContext httpContext, ILogger<Program> logger)
    {
        try
        {
            // Generate a random 32-byte challenge
            var challenge = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(challenge);
            }

            // Store challenge in session for verification
            httpContext.Session.SetString("webauthn_challenge", Convert.ToBase64String(challenge));

            // Get RP ID from current request
            var rpId = httpContext.Request.Host.Host;

            // Return challenge and options
            return Results.Json(new
            {
                challenge = Convert.ToBase64String(challenge),
                timeout = 60000,
                rpId,
                userVerification = "preferred",
                allowCredentials = new object[] { },
                extensions = new object { },
                status = "ok",
                errorMessage = string.Empty,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating assertion options");
            return Results.Json(new { status = "error", errorMessage = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> MakeAssertion(
        HttpContext httpContext,
        ILogger<Program> logger,
        ICommander commander,
        IQuerier querier,
        AssertionRequest request,
        IPasskeyRepository passkeyRepository,
        IInternalQueryStore internalQueryStore)
    {
        try
        {
            // Validate assertion data
            if (request?.Assertion?.Response == null)
            {
                return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid assertion data" } }, statusCode: 200);
            }

            // Get stored challenge from session
            var challengeBase64 = httpContext.Session.GetString("webauthn_challenge");
            if (string.IsNullOrEmpty(challengeBase64))
            {
                return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "No active authentication session" } }, statusCode: 200);
            }

            // Decode credential ID from standard base64 (matching registration format)
            byte[] credentialId;
            try
            {
                credentialId = Convert.FromBase64String(request.Assertion.RawId);
            }
            catch
            {
                return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid credential ID format" } }, statusCode: 200);
            }

            // Decode authenticator data from standard base64
            byte[] authenticatorData;
            try
            {
                authenticatorData = Convert.FromBase64String(request.Assertion.Response.AuthenticatorData);
            }
            catch
            {
                return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid assertion encoding" } }, statusCode: 200);
            }

            // Extract sign count from authenticator data (bytes 33-36)
            if (authenticatorData.Length < 37)
            {
                logger.LogWarning("Authenticator data too short: {Length} bytes (need at least 37)", authenticatorData.Length);
                return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid authenticator data length" } }, statusCode: 200);
            }

            uint signCount = (uint)((authenticatorData[33] << 24) |
                (authenticatorData[34] << 16) |
                (authenticatorData[35] << 8) |
                authenticatorData[36]);

            // Execute authentication command
            var authCommand = new AuthenticateWithPasskeyCommand(credentialId, signCount);
            var commandResult = await commander.Send(authCommand, CancellationToken.None);

            if (commandResult.Status != CommandResultStatus.Succeeded)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Authentication failed" } },
                    statusCode: 200);
            }

            // Look up the passkey by credential ID to get the user ID
            var passkeyMaybe = await passkeyRepository.GetByCredentialIdAsync(credentialId, CancellationToken.None);
            if (passkeyMaybe.HasNoValue)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Could not verify credential" } },
                    statusCode: 200);
            }

            var userId = passkeyMaybe.Value.UserId;

            // Query user details to get email and name
            var userQuery = new GetUserByIdQuery(userId);
            internalQueryStore.StoreQueryRef(userQuery);
            var userResult = await querier.Send(userQuery);

            if (userResult.Status != QueryResultStatus.Succeeded)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "User not found" } },
                    statusCode: 200);
            }

            var userDetails = userResult.Data;

            // Create claims and sign in
            var claims = ClaimsBuilder.BuildClaims(
                userDetails.Id,
                userDetails.EmailAddress,
                userDetails.Name,
                userDetails.SystemRole,
                userDetails.ModeratedDomains,
                userDetails.ModeratedSubdomains,
                userDetails.ViewableSubdomains,
                "passkey");

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true,
            };

            await httpContext.SignOutAsync();
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties);

            // Clear the challenge from session
            httpContext.Session.Remove("webauthn_challenge");

            return Results.Json(new
            {
                url = "/app",
                assertionVerificationResult = new
                {
                    credentialId = request.Assertion.Id,
                    counter = signCount,
                    status = "ok",
                },
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying assertion");
            return Results.Json(new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Authentication failed" } }, statusCode: 200);
        }
    }
}