namespace Spamma.Modules.UserManagement.Client.Contracts;

/// <summary>
/// Error codes for user management domain operations.
/// </summary>
public static class UserManagementErrorCodes
{
    /// <summary>
    /// Error code for suspended accounts.
    /// </summary>
    public const string AccountSuspended = "user_management.account_suspended";

    /// <summary>
    /// Error code for invalid authentication attempts.
    /// </summary>
    public const string InvalidAuthenticationAttempt = "user_management.invalid_authentication_attempt";

    /// <summary>
    /// Error code when user is not suspended.
    /// </summary>
    public const string NotSuspended = "user_management.not_suspended";

    /// <summary>
    /// Error code when user is already suspended.
    /// </summary>
    public const string AlreadySuspended = "user_management.already_suspended";

    /// <summary>
    /// Error code for invalid passkey registration.
    /// </summary>
    public const string InvalidPasskeyRegistration = "user_management.invalid_passkey_registration";

    /// <summary>
    /// Error code when passkey has been revoked.
    /// </summary>
    public const string PasskeyRevoked = "user_management.passkey_revoked";

    /// <summary>
    /// Error code when passkey signature counter decreased (cloning attack).
    /// </summary>
    public const string PasskeyClonedOrInvalid = "user_management.passkey_cloned_or_invalid";

    /// <summary>
    /// Error code when passkey is already revoked.
    /// </summary>
    public const string PasskeyAlreadyRevoked = "user_management.passkey_already_revoked";

    /// <summary>
    /// Error code when passkey is not found.
    /// </summary>
    public const string PasskeyNotFound = "user_management.passkey_not_found";

    /// <summary>
    /// Error code when credential ID is already registered by another user (duplicate).
    /// </summary>
    public const string PasskeyAlreadyRegistered = "user_management.passkey_already_registered";

    /// <summary>
    /// Error code when FIDO2 verification of a passkey fails during registration or authentication.
    /// </summary>
    public const string PasskeyVerificationFailed = "user_management.passkey_verification_failed";

    /// <summary>
    /// Error code when a possible replay attack is detected via sign count validation.
    /// </summary>
    public const string PasskeyReplayAttackDetected = "user_management.passkey_replay_attack_detected";
}