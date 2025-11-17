using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

/// <summary>
/// Business logic for Passkey aggregate.
/// </summary>
public sealed partial class Passkey : AggregateRoot
{
    private DateTime? _lastUsedAt;
    private DateTime? _revokedAt;
    private Guid? _revokedByUserId;

    private Passkey()
    {
    }

    public override Guid Id { get; protected set; }

    internal Guid UserId { get; private set; }

    internal byte[] CredentialId { get; private set; } = [];

    internal byte[] PublicKey { get; private set; } = [];

    internal uint SignCount { get; private set; }

    internal string DisplayName { get; private set; } = string.Empty;

    internal string Algorithm { get; private set; } = string.Empty;

    internal DateTime RegisteredAt { get; private set; }

    internal DateTime LastUsedAt => this._lastUsedAt ?? throw new InvalidOperationException("Passkey has never been used.");

    internal bool HasBeenUsed => this._lastUsedAt.HasValue;

    internal DateTime RevokedAt => this._revokedAt ?? throw new InvalidOperationException("Passkey is not revoked.");

    internal Guid RevokedByUserId => this._revokedByUserId ?? throw new InvalidOperationException("Passkey is not revoked.");

    internal bool IsRevoked => this._revokedAt.HasValue;

    internal static Result<Passkey, BluQubeErrorData> Register(
        Guid userId,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        string displayName,
        string algorithm,
        DateTime registeredAt)
    {
        if (userId == Guid.Empty)
        {
            return Result.Fail<Passkey, BluQubeErrorData>(
                new BluQubeErrorData(
                    UserManagementErrorCodes.InvalidPasskeyRegistration,
                    "User ID cannot be empty"));
        }

        if (credentialId.Length == 0)
        {
            return Result.Fail<Passkey, BluQubeErrorData>(
                new BluQubeErrorData(
                    UserManagementErrorCodes.InvalidPasskeyRegistration,
                    "Credential ID cannot be empty"));
        }

        if (publicKey.Length == 0)
        {
            return Result.Fail<Passkey, BluQubeErrorData>(
                new BluQubeErrorData(
                    UserManagementErrorCodes.InvalidPasskeyRegistration,
                    "Public key cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Fail<Passkey, BluQubeErrorData>(
                new BluQubeErrorData(
                    UserManagementErrorCodes.InvalidPasskeyRegistration,
                    "Display name cannot be empty"));
        }

        var passkey = new Passkey { Id = Guid.NewGuid() };
        var registrationEvent = new PasskeyRegistered(
            passkey.Id,
            userId,
            credentialId,
            publicKey,
            signCount,
            displayName,
            algorithm,
            registeredAt);

        passkey.RaiseEvent(registrationEvent);
        return Result.Ok<Passkey, BluQubeErrorData>(passkey);
    }

    internal ResultWithError<BluQubeErrorData> RecordAuthentication(uint newSignCount, DateTime usedAt)
    {
        if (this.IsRevoked)
        {
            return ResultWithError.Fail(
                new BluQubeErrorData(
                    UserManagementErrorCodes.PasskeyRevoked,
                    "Passkey has been revoked"));
        }

        // Signature counter must never decrease - lower values indicate cloning attacks
        if (newSignCount < this.SignCount)
        {
            return ResultWithError.Fail(
                new BluQubeErrorData(
                    UserManagementErrorCodes.PasskeyClonedOrInvalid,
                    "Sign count decreased - possible credential cloning"));
        }

        var authenticationEvent = new PasskeyAuthenticated(newSignCount, usedAt);
        this.RaiseEvent(authenticationEvent);
        return ResultWithError.Ok<BluQubeErrorData>();
    }

    internal ResultWithError<BluQubeErrorData> Revoke(Guid revokedByUserId, DateTime revokedAt)
    {
        if (this.IsRevoked)
        {
            return ResultWithError.Fail(
                new BluQubeErrorData(
                    UserManagementErrorCodes.PasskeyAlreadyRevoked,
                    "Passkey is already revoked"));
        }

        var revocationEvent = new PasskeyRevoked(revokedByUserId, revokedAt);
        this.RaiseEvent(revocationEvent);
        return ResultWithError.Ok<BluQubeErrorData>();
    }
}