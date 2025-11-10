using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

namespace Spamma.Modules.UserManagement.Tests.Builders;

public class PasskeyBuilder
{
    private Guid? _id = null;
    private Guid _userId = Guid.NewGuid();
    private byte[] _credentialId = new byte[32];
    private byte[] _publicKey = new byte[32];
    private string _displayName = "Test Key";
    private string _algorithm = "ES256";
    private uint _signCount = 0;
    private DateTime _registeredAt = DateTime.UtcNow;
    private bool _isRevoked = false;
    private DateTime? _revokedAt = null;
    private Guid _revokedByUserId = Guid.NewGuid();

    public PasskeyBuilder WithId(Guid id)
    {
        this._id = id;
        return this;
    }

    public PasskeyBuilder WithUserId(Guid userId)
    {
        this._userId = userId;
        return this;
    }

    public PasskeyBuilder WithCredentialId(byte[] credentialId)
    {
        this._credentialId = credentialId;
        return this;
    }

    public PasskeyBuilder WithPublicKey(byte[] publicKey)
    {
        this._publicKey = publicKey;
        return this;
    }

    public PasskeyBuilder WithDisplayName(string displayName)
    {
        this._displayName = displayName;
        return this;
    }

    public PasskeyBuilder WithAlgorithm(string algorithm)
    {
        this._algorithm = algorithm;
        return this;
    }

    public PasskeyBuilder WithSignCount(uint signCount)
    {
        this._signCount = signCount;
        return this;
    }

    public PasskeyBuilder WithRegisteredAt(DateTime registeredAt)
    {
        this._registeredAt = registeredAt;
        return this;
    }

    public PasskeyBuilder AsRevoked(DateTime revokedAt, Guid? revokedByUserId = null)
    {
        this._isRevoked = true;
        this._revokedAt = revokedAt;
        if (revokedByUserId.HasValue)
        {
            this._revokedByUserId = revokedByUserId.Value;
        }

        return this;
    }

    public Passkey Build()
    {
        // Create using the Register method (matching how it's created in production)
        var registerResult = Passkey.Register(
            this._userId,
            this._credentialId,
            this._publicKey,
            this._signCount,
            this._displayName,
            this._algorithm,
            this._registeredAt);

        if (registerResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build passkey: {registerResult.Error.Message}");
        }

        var passkey = registerResult.Value;

        // Set the ID using reflection if a custom ID was provided
        if (this._id.HasValue)
        {
            var idProperty = typeof(Passkey).GetProperty("Id");
            idProperty?.SetValue(passkey, this._id.Value);
        }

        // Apply revocation if needed
        if (this._isRevoked && this._revokedAt.HasValue)
        {
            passkey.Revoke(this._revokedByUserId, this._revokedAt.Value);
        }

        return passkey;
    }
}