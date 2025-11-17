using FluentAssertions;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

namespace Spamma.Modules.UserManagement.Tests.Domain;

public class PasskeySecurityTests
{
    [Fact]
    public void RecordAuthentication_SignCountIncreases_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0x05, 0x06, 0x07, 0x08 };
        var initialSignCount = 5u;

        var passkeyResult = Passkey.Register(
            userId,
            credentialId,
            publicKey,
            initialSignCount,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        passkeyResult.IsSuccess.Should().BeTrue();
        var passkey = passkeyResult.Value;

        // Act - Sign count increases from 5 to 6 (valid)
        var result = passkey.RecordAuthentication(6, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue("sign count increased, indicating legitimate authentication");
        passkey.SignCount.Should().Be(6);
    }

    [Fact]
    public void RecordAuthentication_SignCountDecreases_FailsWithCloningError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0x05, 0x06, 0x07, 0x08 };
        var initialSignCount = 10u;

        var passkeyResult = Passkey.Register(
            userId,
            credentialId,
            publicKey,
            initialSignCount,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        passkeyResult.IsSuccess.Should().BeTrue();
        var passkey = passkeyResult.Value;

        // Act - Sign count DECREASES from 10 to 5 (credential cloning attack)
        var result = passkey.RecordAuthentication(5, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue("sign count decreased indicates credential cloning");
        result.Error.Code.Should().Be(UserManagementErrorCodes.PasskeyClonedOrInvalid);
        result.Error.Message.Should().Contain("Sign count decreased");
        passkey.SignCount.Should().Be(10, "sign count should not change on failed authentication");
    }

    [Fact]
    public void RecordAuthentication_SignCountSameValue_SucceedsForNonCounterAuthenticators()
    {
        // Arrange
        // NOTE: WebAuthn spec allows sign count to remain 0 for authenticators that don't support counters
        // See: https://www.w3.org/TR/webauthn-2/#sctn-sign-counter
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0x05, 0x06, 0x07, 0x08 };
        var initialSignCount = 0u; // Non-counter authenticator

        var passkeyResult = Passkey.Register(
            userId,
            credentialId,
            publicKey,
            initialSignCount,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        passkeyResult.IsSuccess.Should().BeTrue();
        var passkey = passkeyResult.Value;

        // Act - Sign count stays at 0 (valid for non-counter authenticators)
        var result = passkey.RecordAuthentication(0, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue("non-counter authenticators can have sign count stay at 0 per WebAuthn spec");
        passkey.SignCount.Should().Be(0);
    }

    [Fact]
    public void RecordAuthentication_RevokedPasskey_FailsWithRevokedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0x05, 0x06, 0x07, 0x08 };
        var initialSignCount = 5u;

        var passkeyResult = Passkey.Register(
            userId,
            credentialId,
            publicKey,
            initialSignCount,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        passkeyResult.IsSuccess.Should().BeTrue();
        var passkey = passkeyResult.Value;

        // Revoke the passkey
        var revokeResult = passkey.Revoke(userId, DateTime.UtcNow);
        revokeResult.IsSuccess.Should().BeTrue();

        // Act - Attempt to authenticate with revoked passkey
        var result = passkey.RecordAuthentication(6, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue("revoked passkeys cannot be used for authentication");
        result.Error.Code.Should().Be(UserManagementErrorCodes.PasskeyRevoked);
        result.Error.Message.Should().Contain("revoked");
    }

    [Fact]
    public void Register_EmptyCredentialId_FailsValidation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyCredentialId = Array.Empty<byte>();
        var publicKey = new byte[] { 0x05, 0x06, 0x07, 0x08 };

        // Act
        var result = Passkey.Register(
            userId,
            emptyCredentialId,
            publicKey,
            0,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue("empty credential ID should be rejected");
        result.Error.Code.Should().Be(UserManagementErrorCodes.InvalidPasskeyRegistration);
        result.Error.Message.Should().Contain("Credential ID cannot be empty");
    }

    [Fact]
    public void Register_EmptyPublicKey_FailsValidation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var emptyPublicKey = Array.Empty<byte>();

        // Act
        var result = Passkey.Register(
            userId,
            credentialId,
            emptyPublicKey,
            0,
            "My Security Key",
            "ES256",
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue("empty public key should be rejected");
        result.Error.Code.Should().Be(UserManagementErrorCodes.InvalidPasskeyRegistration);
        result.Error.Message.Should().Contain("Public key cannot be empty");
    }
}