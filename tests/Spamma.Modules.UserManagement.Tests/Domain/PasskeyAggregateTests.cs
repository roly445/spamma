using FluentAssertions;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;
using Spamma.Modules.UserManagement.Tests.Builders;

namespace Spamma.Modules.UserManagement.Tests.Domain;

/// <summary>
/// Domain tests for the Passkey aggregate root.
/// Tests business logic for passkey registration, authentication, and revocation using verification patterns.
/// </summary>
public class PasskeyAggregateTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly byte[] _credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
    private readonly byte[] _publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    [Fact]
    public void Register_WithValidParameters_CreatesPasskeySuccessfully()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "My Security Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(this._userId);
        result.Value.DisplayName.Should().Be("My Security Key");
        result.Value.Algorithm.Should().Be("ES256");
        result.Value.SignCount.Should().Be(0);
        result.Value.IsRevoked.Should().BeFalse();
        result.Value.RegisteredAt.Should().Be(this._fixedUtcNow);
        result.Value.LastUsedAt.Should().BeNull();
        result.Value.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Register_WithEmptyUserId_ReturnsFail()
    {
        // Act
        var result = Passkey.Register(
            Guid.Empty,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "My Security Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithEmptyCredentialId_ReturnsFail()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            new byte[0],
            this._publicKey,
            signCount: 0,
            displayName: "My Security Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithEmptyPublicKey_ReturnsFail()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            new byte[0],
            signCount: 0,
            displayName: "My Security Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithEmptyDisplayName_ReturnsFail()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: string.Empty,
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithWhitespaceDisplayName_ReturnsFail()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "   ",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_GeneratesUniqueId_ForEachCall()
    {
        // Act
        var result1 = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "Key 1",
            algorithm: "ES256",
            this._fixedUtcNow);

        var result2 = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "Key 2",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }

    [Fact]
    public void RecordAuthentication_WithIncrementedSignCount_UpdatesPasskeySuccessfully()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(5)
            .Build();

        // Act
        var result = passkey.RecordAuthentication(newSignCount: 6, usedAt: this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        passkey.SignCount.Should().Be(6);
        passkey.LastUsedAt.Should().Be(this._fixedUtcNow);
    }

    [Fact]
    public void RecordAuthentication_WithSameSignCount_UpdatesLastUsedAt()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(5)
            .Build();

        var laterTime = this._fixedUtcNow.AddSeconds(30);

        // Act
        var result = passkey.RecordAuthentication(newSignCount: 5, usedAt: laterTime);

        // Verify
        result.IsSuccess.Should().BeTrue();
        passkey.SignCount.Should().Be(5);
        passkey.LastUsedAt.Should().Be(laterTime);
    }

    [Fact]
    public void RecordAuthentication_WithDecreasedSignCount_ReturnsFail()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(10)
            .Build();

        // Act
        var result = passkey.RecordAuthentication(newSignCount: 9, usedAt: this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
        passkey.SignCount.Should().Be(10); // Unchanged
    }

    [Fact]
    public void RecordAuthentication_OnRevokedPasskey_ReturnsFail()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(5)
            .AsRevoked(this._fixedUtcNow.AddHours(-1), this._userId)
            .Build();

        // Act
        var result = passkey.RecordAuthentication(newSignCount: 6, usedAt: this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
        passkey.LastUsedAt.Should().BeNull(); // Not updated
    }

    [Fact]
    public void Revoke_WithValidParameters_RevokesPasskeySuccessfully()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .Build();

        var revokedByUserId = Guid.NewGuid();

        // Act
        var result = passkey.Revoke(revokedByUserId, this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        passkey.IsRevoked.Should().BeTrue();
        passkey.RevokedAt.Should().Be(this._fixedUtcNow);
        passkey.RevokedByUserId.Should().Be(revokedByUserId);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ReturnsFail()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .AsRevoked(this._fixedUtcNow.AddHours(-1), this._userId)
            .Build();

        var revokedByUserId = Guid.NewGuid();

        // Act
        var result = passkey.Revoke(revokedByUserId, this._fixedUtcNow);

        // Verify
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Passkey_InitialState_IsNotRevoked()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: 0,
            displayName: "Test Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.Value.IsRevoked.Should().BeFalse();
        result.Value.RevokedAt.Should().BeNull();
        result.Value.RevokedByUserId.Should().BeNull();
        result.Value.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public void Passkey_StoresCredentialIdCorrectly()
    {
        // Arrange
        var customCredentialId = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA };

        // Act
        var result = Passkey.Register(
            this._userId,
            customCredentialId,
            this._publicKey,
            signCount: 0,
            displayName: "Test Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.Value.CredentialId.Should().Equal(customCredentialId);
    }

    [Fact]
    public void Passkey_StoresPublicKeyCorrectly()
    {
        // Arrange
        var customPublicKey = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };

        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            customPublicKey,
            signCount: 0,
            displayName: "Test Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.Value.PublicKey.Should().Equal(customPublicKey);
    }

    [Fact]
    public void RecordAuthentication_MultipleIncrements_MaintainsCorrectSignCount()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(0)
            .Build();

        var time1 = this._fixedUtcNow;
        var time2 = this._fixedUtcNow.AddSeconds(10);
        var time3 = this._fixedUtcNow.AddSeconds(20);

        // Act
        passkey.RecordAuthentication(newSignCount: 1, usedAt: time1);
        passkey.RecordAuthentication(newSignCount: 3, usedAt: time2);
        passkey.RecordAuthentication(newSignCount: 5, usedAt: time3);

        // Verify
        passkey.SignCount.Should().Be(5);
        passkey.LastUsedAt.Should().Be(time3);
    }

    [Fact]
    public void RecordAuthentication_WithZeroSignCountIncrement_Succeeds()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(0)
            .Build();

        // Act - Sign count stays at 0 (first authentication)
        var result = passkey.RecordAuthentication(newSignCount: 0, usedAt: this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        passkey.SignCount.Should().Be(0);
    }

    [Fact]
    public void RecordAuthentication_WithLargeSignCountJump_Succeeds()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithSignCount(10)
            .Build();

        // Act - Large jump is acceptable as long as it's greater than current
        var result = passkey.RecordAuthentication(newSignCount: 1000, usedAt: this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        passkey.SignCount.Should().Be(1000);
    }

    [Fact]
    public void Register_WithHighSignCount_Accepted()
    {
        // Act
        var result = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            signCount: uint.MaxValue,
            displayName: "High Count Key",
            algorithm: "ES256",
            this._fixedUtcNow);

        // Verify
        result.IsSuccess.Should().BeTrue();
        result.Value.SignCount.Should().Be(uint.MaxValue);
    }

    [Fact]
    public void Revoke_MarksRevokedProperties_Correctly()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithDisplayName("My Key")
            .WithAlgorithm("RS256")
            .WithSignCount(42)
            .Build();

        var revokedByUserId = Guid.NewGuid();
        var revokedAt = this._fixedUtcNow;

        // Act
        passkey.Revoke(revokedByUserId, revokedAt);

        // Verify revocation-specific properties are set correctly
        passkey.IsRevoked.Should().BeTrue();
        passkey.RevokedAt.Should().Be(revokedAt);
        passkey.RevokedByUserId.Should().Be(revokedByUserId);
    }

    [Fact]
    public void Revoke_DoesNotModifyOtherPasskeyProperties()
    {
        // Arrange
        var passkey = new PasskeyBuilder()
            .WithUserId(this._userId)
            .WithCredentialId(this._credentialId)
            .WithDisplayName("My Key")
            .WithAlgorithm("RS256")
            .WithSignCount(42)
            .Build();

        var originalUserId = passkey.UserId;
        var originalDisplayName = passkey.DisplayName;
        var originalAlgorithm = passkey.Algorithm;
        var originalSignCount = passkey.SignCount;

        var revokedByUserId = Guid.NewGuid();

        // Act
        passkey.Revoke(revokedByUserId, this._fixedUtcNow);

        // Verify non-revocation properties unchanged
        passkey.UserId.Should().Be(originalUserId);
        passkey.DisplayName.Should().Be(originalDisplayName);
        passkey.Algorithm.Should().Be(originalAlgorithm);
        passkey.SignCount.Should().Be(originalSignCount);
    }
}