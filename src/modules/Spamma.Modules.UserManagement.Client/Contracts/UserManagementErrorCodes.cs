namespace Spamma.Modules.UserManagement.Client.Contracts;

public static class UserManagementErrorCodes
{
    public const string AccountSuspended = "user_management.account_suspended";

    public const string InvalidAuthenticationAttempt = "user_management.invalid_authentication_attempt";

    public const string NotSuspended = "user_management.not_suspended";

    public const string AlreadySuspended = "user_management.already_suspended";

    public const string InvalidPasskeyRegistration = "user_management.invalid_passkey_registration";

    public const string PasskeyRevoked = "user_management.passkey_revoked";

    public const string PasskeyClonedOrInvalid = "user_management.passkey_cloned_or_invalid";

    public const string PasskeyAlreadyRevoked = "user_management.passkey_already_revoked";

    public const string PasskeyNotFound = "user_management.passkey_not_found";

    public const string PasskeyAlreadyRegistered = "user_management.passkey_already_registered";

    public const string PasskeyVerificationFailed = "user_management.passkey_verification_failed";

    public const string PasskeyReplayAttackDetected = "user_management.passkey_replay_attack_detected";

    public const string ApiKeyNotFound = "user_management.api_key_not_found";

    public const string ApiKeyAlreadyRevoked = "user_management.api_key_already_revoked";

    public const string AccessDenied = "user_management.access_denied";

    public const string ApiKeyInvalidName = "user_management.api_key_invalid_name";

    public const string ApiKeyInvalidPrefix = "user_management.api_key_invalid_prefix";

    public const string ApiKeyInvalidHash = "user_management.api_key_invalid_hash";
}