namespace Spamma.Modules.UserManagement.Infrastructure.ReadModels;

/// <summary>
/// Read model for querying passkeys by credential ID or user ID.
/// Denormalizes passkey data from event stream into a searchable form.
/// </summary>
public class PasskeyLookup
{
    /// <summary>
    /// Gets or sets the passkey aggregate ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID who owns this passkey.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the WebAuthn credential ID (raw bytes) stored in base64 for persistence.
    /// Used to match assertions during authentication.
    /// </summary>
    public byte[] CredentialId { get; set; } = [];

    /// <summary>
    /// Gets or sets the display name for the passkey (e.g., "Work MacBook").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the algorithm identifier (e.g., "-7" for ES256, "-257" for RS256).
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the passkey was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last authentication using this passkey.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the passkey has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the passkey was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}