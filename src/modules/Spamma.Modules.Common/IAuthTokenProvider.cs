using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    Result<string> GenerateVerificationToken(VerificationTokenModel model);

    Result<VerificationTokenModel> ProcessVerificationToken(string token);

    Result<string> GenerateAuthenticationToken(AuthenticationTokenModel model);

    Result<AuthenticationTokenModel> ProcessAuthenticationToken(string token);

    string GenerateAuthenticatedJwt(Guid userId);

    public record VerificationTokenModel(
        Guid UserId,
        Guid SecurityStamp,
        DateTime WhenCreated,
        Guid EmailId);

    public record AuthenticationTokenModel(
        Guid UserId,
        Guid SecurityStamp,
        DateTime WhenCreated,
        Guid AuthenticationAttemptId);
}

public class AuthTokenProvider(IOptions<Settings> settings) : IAuthTokenProvider
{
    private const string UserIdClaimType = "spamma-user-id";
    private const string SecurityTokenClaimType = "spamma-security-token";
    private readonly Settings _settings = settings.Value;

    public Result<string> GenerateVerificationToken(IAuthTokenProvider.VerificationTokenModel model)
    {
        return this.GetToken(model.UserId, model.SecurityStamp, model.WhenCreated, new Dictionary<string, string>
        {
            { "email-token", model.EmailId.ToString() },
        });
    }

    public Result<IAuthTokenProvider.VerificationTokenModel> ProcessVerificationToken(string token)
    {
        var tokenResult = this.ProcessToken(token);
        if (tokenResult.IsFailure)
        {
            return Result.Fail<IAuthTokenProvider.VerificationTokenModel>();
        }

        Guid emailToken;
        if (tokenResult.Value.OtherData.TryGetValue("email-token", out var rawEmailToken))
        {
            if (Guid.TryParse(rawEmailToken, out var parsedEmailToken))
            {
                emailToken = parsedEmailToken;
            }
            else
            {
                return Result.Fail<IAuthTokenProvider.VerificationTokenModel>();
            }
        }
        else
        {
            return Result.Fail<IAuthTokenProvider.VerificationTokenModel>();
        }

        return Result.Ok(new IAuthTokenProvider.VerificationTokenModel(
            tokenResult.Value.UserId,
            tokenResult.Value.SecurityStamp,
            tokenResult.Value.SecurityToken.ValidFrom, // WhenCreated is not part of the token, so we use current time
            emailToken));
    }

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

    public string GenerateAuthenticatedJwt(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._settings.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: this._settings.JwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24), // Token expires in 24 hours
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Result<string> GetToken(Guid userId, Guid securityStamp, DateTime whenCreated,
        IReadOnlyDictionary<string, string>? otherData = null)
    {
        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(this._settings.SigningKeyBase64));

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
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Convert.FromBase64String(this._settings.SigningKeyBase64);

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
        catch
        {
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