using BluQube.Commands;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

public partial class Passkey : AggregateRoot
{
    private Passkey()
    {
    }

    public override Guid Id { get; protected set; }

    public Guid UserId { get; private set; }

    public byte[] CredentialId { get; private set; } = [];

    public byte[] PublicKey { get; private set; } = [];

    public uint SignCount { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string Algorithm { get; private set; } = string.Empty;

    public bool IsRevoked { get; private set; }

    public DateTime RegisteredAt { get; private set; }

    public DateTime? LastUsedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public static Result<Passkey, BluQubeErrorData> Register(
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

    public ResultWithError<BluQubeErrorData> RecordAuthentication(uint newSignCount, DateTime usedAt)
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

    public ResultWithError<BluQubeErrorData> Revoke(Guid revokedByUserId, DateTime revokedAt)
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