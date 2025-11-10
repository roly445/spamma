using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

/// <summary>
/// Utility service for JWT token validation.
/// </summary>
public class JwtValidationService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtValidationService"/> class.
    /// </summary>
    /// <param name="issuer">The JWT issuer.</param>
    /// <param name="audience">The JWT audience.</param>
    /// <param name="key">The JWT signing key.</param>
    public JwtValidationService(string issuer, string audience, string key)
    {
        this._issuer = issuer;
        this._audience = audience;
        this._signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>The claims principal if valid, null otherwise.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = this._issuer,
            ValidAudience = this._audience,
            IssuerSigningKey = this._signingKey,
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}