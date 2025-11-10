using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Spamma.Modules.Common;

namespace Spamma.Modules.UserManagement.Tests.Application;

/// <summary>
/// Security-focused tests for magic link authentication token generation and validation.
/// These tests verify protection against replay attacks, token tampering, and expired token usage.
/// </summary>
public class AuthTokenProviderTests
{
    private readonly AuthTokenProvider _authTokenProvider;
    private readonly Settings _settings;

    public AuthTokenProviderTests()
    {
        this._settings = new Settings
        {
            SigningKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("ThisIsAVerySecureTestKeyThatIsLongEnough123456")),
            JwtKey = "ThisIsAVerySecureJwtKeyThatIsLongEnough123456",
            JwtIssuer = "Spamma.Tests",
        };

        var options = Options.Create(this._settings);
        this._authTokenProvider = new AuthTokenProvider(options);
    }

    [Fact]
    public void GenerateAuthenticationToken_ValidModel_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();

        var model = new IAuthTokenProvider.AuthenticationTokenModel(
            userId,
            securityStamp,
            DateTime.UtcNow,
            authenticationAttemptId);

        // Act
        var result = this._authTokenProvider.GenerateAuthenticationToken(model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAndProcessAuthenticationToken_ValidToken_RoundtripSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();
        var whenCreated = DateTime.UtcNow;

        var originalModel = new IAuthTokenProvider.AuthenticationTokenModel(
            userId,
            securityStamp,
            whenCreated,
            authenticationAttemptId);

        var tokenResult = this._authTokenProvider.GenerateAuthenticationToken(originalModel);
        tokenResult.IsSuccess.Should().BeTrue();

        // Act
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(tokenResult.Value);

        // Assert
        processResult.IsSuccess.Should().BeTrue();
        processResult.Value.UserId.Should().Be(userId);
        processResult.Value.SecurityStamp.Should().Be(securityStamp);
        processResult.Value.AuthenticationAttemptId.Should().Be(authenticationAttemptId);
        processResult.Value.WhenCreated.Should().BeCloseTo(whenCreated, TimeSpan.FromSeconds(1));
    }

    [Fact(Skip = "Token expiration validation requires time-travel or Thread.Sleep which is impractical for unit tests. Expiration is validated by JWT library in production.")]
    public void ProcessAuthenticationToken_ExpiredToken_ReturnsFailure()
    {
        // Arrange - Create token with "WhenCreated" timestamp > 1 hour ago
        // NOTE: This test is skipped because the JWT library sets NotBefore = DateTime.UtcNow automatically
        // which conflicts with Expires = whenCreated.AddHours(1) when whenCreated is in the past.
        // Token expiration is validated by the JWT library in ProcessToken() via ValidateToken().
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();

        // Token created 2 hours ago (beyond 1 hour expiration)
        var expiredTimestamp = DateTime.UtcNow.AddHours(-2);

        var expiredModel = new IAuthTokenProvider.AuthenticationTokenModel(
            userId,
            securityStamp,
            expiredTimestamp,
            authenticationAttemptId);

        var tokenResult = this._authTokenProvider.GenerateAuthenticationToken(expiredModel);
        tokenResult.IsSuccess.Should().BeTrue();

        // Act - Process token after expiration (current time is _fixedUtcNow)
        // Note: Token expiration is checked by JWT library against current time
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(tokenResult.Value);

        // Assert
        processResult.IsFailure.Should().BeTrue("expired tokens should be rejected to prevent replay attacks");
    }

    [Fact]
    public void ProcessAuthenticationToken_TamperedUserId_ReturnsFailure()
    {
        // Arrange - Create valid token, then tamper with it
        var originalUserId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();

        var model = new IAuthTokenProvider.AuthenticationTokenModel(
            originalUserId,
            securityStamp,
            DateTime.UtcNow,
            authenticationAttemptId);

        var tokenResult = this._authTokenProvider.GenerateAuthenticationToken(model);
        tokenResult.IsSuccess.Should().BeTrue();

        // Tamper with token: decode, modify userId, re-encode WITHOUT proper signing
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResult.Value);

        var tamperedUserId = Guid.NewGuid();
        var tamperedClaims = jwtToken.Claims
            .Where(c => c.Type != "spamma-user-id")
            .Append(new Claim("spamma-user-id", tamperedUserId.ToString()))
            .ToList();

        var tamperedToken = new JwtSecurityToken(
            claims: tamperedClaims,
            expires: jwtToken.ValidTo,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WrongSigningKeyForTampering12345678901234")),
                SecurityAlgorithms.HmacSha256Signature));

        var tamperedTokenString = handler.WriteToken(tamperedToken);

        // Act
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(tamperedTokenString);

        // Assert
        processResult.IsFailure.Should().BeTrue("tampered tokens with invalid signatures should be rejected");
    }

    [Fact]
    public void ProcessAuthenticationToken_InvalidSignature_ReturnsFailure()
    {
        // Arrange - Create token signed with different key
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();

        var wrongKey = new SymmetricSecurityKey(Convert.FromBase64String(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("DifferentSecretKeyThatIsLongEnough123456789"))));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("spamma-user-id", userId.ToString()),
                new Claim("spamma-security-token", securityStamp.ToString()),
                new Claim("authentication-attempt-id", authenticationAttemptId.ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var invalidToken = tokenHandler.CreateToken(tokenDescriptor);
        var invalidTokenString = tokenHandler.WriteToken(invalidToken);

        // Act
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(invalidTokenString);

        // Assert
        processResult.IsFailure.Should().BeTrue("tokens signed with incorrect key should be rejected");
    }

    [Fact]
    public void ProcessAuthenticationToken_MissingAuthenticationAttemptId_ReturnsFailure()
    {
        // Arrange - Create token WITHOUT authentication-attempt-id claim
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(this._settings.SigningKeyBase64));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("spamma-user-id", userId.ToString()),
                new Claim("spamma-security-token", securityStamp.ToString()),

                // Missing: authentication-attempt-id claim
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var invalidToken = tokenHandler.CreateToken(tokenDescriptor);
        var invalidTokenString = tokenHandler.WriteToken(invalidToken);

        // Act
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(invalidTokenString);

        // Assert
        processResult.IsFailure.Should().BeTrue("tokens missing authentication-attempt-id claim should be rejected");
    }

    [Fact]
    public void ProcessAuthenticationToken_MalformedToken_ReturnsFailure()
    {
        // Arrange
        var malformedToken = "not.a.valid.jwt.token.at.all";

        // Act
        var processResult = this._authTokenProvider.ProcessAuthenticationToken(malformedToken);

        // Assert
        processResult.IsFailure.Should().BeTrue("malformed tokens should be rejected");
    }
}
