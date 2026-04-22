using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ResultMonad;

namespace Spamma.Modules.Common;

public enum ErrorCodes
{
    TokenNotValid,
}

public interface IAuthTokenProvider
{
    Result<string> GenerateAuthenticationToken(AuthenticationTokenModel model);

    Result<AuthenticationTokenModel> ProcessAuthenticationToken(string token);

    public record AuthenticationTokenModel(
        Guid UserId,
        Guid SecurityStamp,
        DateTime WhenCreated,
        Guid AuthenticationAttemptId);
}

public class AuthTokenProvider(IOptions<Settings> settings, ILogger<AuthTokenProvider> logger) : IAuthTokenProvider
{
    private const string UserIdClaimType = "spamma-user-id";
    private const string SecurityTokenClaimType = "spamma-security-token";
    private readonly Settings _settings = settings.Value;

    public Result<string> GenerateAuthenticationToken(IAuthTokenProvider.AuthenticationTokenModel model)
    {
        return this.GetToken(model.UserId, model.SecurityStamp, model.WhenCreated, new Dictionary<string, string>
        {
            { "authentication-attempt-id", model.AuthenticationAttemptId.ToString() },
        });
    }

    public Result<IAuthTokenProvider.AuthenticationTokenModel> ProcessAuthenticationToken(string token)
    {
        var tokenResult = this.ProcessToken(token);
        if (tokenResult.IsFailure)
        {
            return Result.Fail<IAuthTokenProvider.AuthenticationTokenModel>();
        }

        Guid authenticationAttemptId;
        if (tokenResult.Value.OtherData.TryGetValue("authentication-attempt-id", out var rawAuthenticationAttemptId))
        {
            if (Guid.TryParse(rawAuthenticationAttemptId, out var parsedAuthenticationAttemptId))
            {
                authenticationAttemptId = parsedAuthenticationAttemptId;
            }
            else
            {
                return Result.Fail<IAuthTokenProvider.AuthenticationTokenModel>();
            }
        }
        else
        {
            return Result.Fail<IAuthTokenProvider.AuthenticationTokenModel>();
        }

        return Result.Ok(new IAuthTokenProvider.AuthenticationTokenModel(
            tokenResult.Value.UserId,
            tokenResult.Value.SecurityStamp,
            tokenResult.Value.SecurityToken.ValidFrom,
            authenticationAttemptId));
    }

    private Result<string> GetToken(Guid userId, Guid securityStamp, DateTime whenCreated,
        IReadOnlyDictionary<string, string>? otherData = null)
    {
        if (string.IsNullOrWhiteSpace(this._settings.SigningKeyBase64))
        {
            logger.LogError("SigningKeyBase64 is not configured — cannot generate authentication token. Run the setup wizard to configure the signing key.");
            return Result.Fail<string>();
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(this._settings.SigningKeyBase64);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "SigningKeyBase64 is not valid Base64 — cannot generate authentication token");
            return Result.Fail<string>();
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(UserIdClaimType, userId.ToString()),
                new Claim(SecurityTokenClaimType, securityStamp.ToString()),
            }.Concat(otherData?.Select(kv => new Claim(kv.Key, kv.Value)) ?? [])),
            Expires = whenCreated.AddHours(1),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Embed GUIDs in the token payload
        var jwtToken = tokenHandler.WriteToken(token);

        return Result.Ok(jwtToken);
    }

    private Result<TokenResult, ErrorData> ProcessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(this._settings.SigningKeyBase64))
        {
            logger.LogError("SigningKeyBase64 is not configured — cannot validate authentication token");
            return Result.Fail<TokenResult, ErrorData>(new ErrorData(ErrorCodes.TokenNotValid, "Signing key not configured"));
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(this._settings.SigningKeyBase64);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "SigningKeyBase64 is not valid Base64 — cannot validate authentication token");
            return Result.Fail<TokenResult, ErrorData>(new ErrorData(ErrorCodes.TokenNotValid, "Token not valid"));
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
        };

        ClaimsPrincipal claimsPrincipal;
        SecurityToken securityToken;
        try
        {
            claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT token validation failed — token rejected as invalid");
            return Result.Fail<TokenResult, ErrorData>(new ErrorData(ErrorCodes.TokenNotValid, "Token not valid"));
        }

        if (claimsPrincipal.Claims.All(x => x.Type != UserIdClaimType) || claimsPrincipal.Claims.All(x => x.Type != SecurityTokenClaimType))
        {
            return Result.Fail<TokenResult, ErrorData>(new ErrorData(ErrorCodes.TokenNotValid, "Token not valid"));
        }

        if (!Guid.TryParse(claimsPrincipal.Claims.Single(x => x.Type == UserIdClaimType).Value, out var userId) ||
            !Guid.TryParse(claimsPrincipal.Claims.Single(x => x.Type == SecurityTokenClaimType).Value, out var securityStamp))
        {
            return Result.Fail<TokenResult, ErrorData>(new ErrorData(ErrorCodes.TokenNotValid, "Token not valid"));
        }

        return Result.Ok<TokenResult, ErrorData>(
            new TokenResult(userId, securityStamp,
                securityToken,
                claimsPrincipal.Claims
                    .Where(x => x.Type != UserIdClaimType && x.Type != SecurityTokenClaimType)
                    .ToDictionary(x => x.Type, x => x.Value)));
    }

    public sealed class ErrorData(ErrorCodes codes, string message)
    {
        public ErrorData(ErrorCodes codes)
            : this(codes, codes.ToString())
        {
        }

        public ErrorCodes Codes { get; } = codes;

        public string Message { get; } = message;
    }

    private sealed record TokenResult(Guid UserId, Guid SecurityStamp, SecurityToken SecurityToken, IReadOnlyDictionary<string, string> OtherData);
}