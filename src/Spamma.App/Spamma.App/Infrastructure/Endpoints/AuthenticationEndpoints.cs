using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Spamma.App.Infrastructure.Contracts;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
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
            var challenge = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(challenge);
            }

            httpContext.Session.SetString("webauthn_challenge", Convert.ToBase64String(challenge));

            var rpId = httpContext.Request.Host.Host;

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
        ICommandRunner commander,
        IQueryRunner querier,
        AssertionRequest request,
        IInternalQueryStore internalQueryStore)
    {
        try
        {
            if (request?.Assertion?.Response == null)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid assertion data" } },
                    statusCode: 200);
            }

            var challengeBase64 = httpContext.Session.GetString("webauthn_challenge");
            if (string.IsNullOrEmpty(challengeBase64))
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "No active authentication session" } },
                    statusCode: 200);
            }

            byte[] credentialId;
            byte[] authenticatorData;
            byte[] clientDataJson;
            byte[] signature;

            try
            {
                credentialId = Convert.FromBase64String(request.Assertion.RawId);
                authenticatorData = Convert.FromBase64String(request.Assertion.Response.AuthenticatorData);
                clientDataJson = Convert.FromBase64String(request.Assertion.Response.ClientDataJson);
                signature = Convert.FromBase64String(request.Assertion.Response.Signature);
            }
            catch
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid assertion encoding" } },
                    statusCode: 200);
            }

            if (authenticatorData.Length < 37)
            {
                logger.LogWarning("Authenticator data too short: {Length} bytes (need at least 37)", authenticatorData.Length);
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Invalid authenticator data length" } },
                    statusCode: 200);
            }

            uint signCount = (uint)((authenticatorData[33] << 24) |
                (authenticatorData[34] << 16) |
                (authenticatorData[35] << 8) |
                authenticatorData[36]);

            var expectedOrigin = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var expectedRpId = httpContext.Request.Host.Host;

            var authCommand = new AuthenticateWithPasskeyCommand(
                CredentialId: credentialId,
                SignCount: signCount,
                AuthenticatorData: authenticatorData,
                ClientDataJson: clientDataJson,
                Signature: signature,
                ExpectedChallengeBase64: challengeBase64,
                ExpectedOrigin: expectedOrigin,
                ExpectedRpId: expectedRpId);

            var commandResult = await commander.Send(authCommand, CancellationToken.None);

            if (commandResult.Status != CommandResultStatus.Succeeded)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Authentication failed" } },
                    statusCode: 200);
            }

            var query = new GetPasskeyByCredentialIdQuery(credentialId);
            var queryResult = await querier.Send(query, CancellationToken.None);

            if (queryResult.Status != QueryResultStatus.Succeeded)
            {
                return Results.Json(
                    new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Could not verify credential" } },
                    statusCode: 200);
            }

            var passkeyData = queryResult.Data;
            var userId = passkeyData.UserId;

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
            return Results.Json(
                new { url = string.Empty, assertionVerificationResult = new { status = "error", errorMessage = "Authentication failed" } },
                statusCode: 200);
        }
    }
}