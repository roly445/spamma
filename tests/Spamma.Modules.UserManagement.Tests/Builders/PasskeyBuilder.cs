using ResultMonad;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;

namespace Spamma.Modules.UserManagement.Tests.Builders;

/// <summary>
/// Fluent builder for creating Passkey aggregates for testing.
/// </summary>
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
    private DateTime? _lastUsedAt = null;
    private bool _isRevoked = false;
    private DateTime? _revokedAt = null;
    private Guid _revokedByUserId = Guid.NewGuid();

    /// <summary>
    /// Sets the passkey ID.
    /// </summary>
    public PasskeyBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the user ID that owns this passkey.
    /// </summary>
    public PasskeyBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the credential ID (raw bytes).
    /// </summary>
    public PasskeyBuilder WithCredentialId(byte[] credentialId)
    {
        _credentialId = credentialId;
        return this;
    }

    /// <summary>
    /// Sets the public key bytes.
    /// </summary>
    public PasskeyBuilder WithPublicKey(byte[] publicKey)
    {
        _publicKey = publicKey;
        return this;
    }

    /// <summary>
    /// Sets the display name for the passkey.
    /// </summary>
    public PasskeyBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <summary>
    /// Sets the algorithm (e.g., "ES256").
    /// </summary>
    public PasskeyBuilder WithAlgorithm(string algorithm)
    {
        _algorithm = algorithm;
        return this;
    }

    /// <summary>
    /// Sets the current sign count.
    /// </summary>
    public PasskeyBuilder WithSignCount(uint signCount)
    {
        _signCount = signCount;
        return this;
    }

    /// <summary>
    /// Sets the registration timestamp.
    /// </summary>
    public PasskeyBuilder WithRegisteredAt(DateTime registeredAt)
    {
        _registeredAt = registeredAt;
        return this;
    }

    /// <summary>
    /// Sets the last used timestamp.
    /// </summary>
    public PasskeyBuilder WithLastUsedAt(DateTime? lastUsedAt)
    {
        _lastUsedAt = lastUsedAt;
        return this;
    }

    /// <summary>
    /// Marks the passkey as revoked.
    /// </summary>
    public PasskeyBuilder AsRevoked(DateTime revokedAt, Guid? revokedByUserId = null)
    {
        _isRevoked = true;
        _revokedAt = revokedAt;
        if (revokedByUserId.HasValue)
        {
            _revokedByUserId = revokedByUserId.Value;
        }

        return this;
    }

    /// <summary>
    /// Builds and returns the Passkey aggregate.
    /// </summary>
    public Passkey Build()
    {
        // Create using the Register method (matching how it's created in production)
        var registerResult = Passkey.Register(
            _userId,
            _credentialId,
            _publicKey,
            _signCount,
            _displayName,
            _algorithm,
            _registeredAt);

        if (registerResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build passkey: {registerResult.Error.Message}");
        }

        var passkey = registerResult.Value;

        // Set the ID using reflection if a custom ID was provided
        if (_id.HasValue)
        {
            var idProperty = typeof(Passkey).GetProperty("Id");
            idProperty?.SetValue(passkey, _id.Value);
        }

        // Apply revocation if needed
        if (_isRevoked && _revokedAt.HasValue)
        {
            passkey.Revoke(_revokedByUserId, _revokedAt.Value);
        }

        return passkey;
    }
}
