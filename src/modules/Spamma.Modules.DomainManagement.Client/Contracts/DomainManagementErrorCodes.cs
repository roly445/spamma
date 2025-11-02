namespace Spamma.Modules.DomainManagement.Client.Contracts;

public static class DomainManagementErrorCodes
{
    public const string UserAlreadyModerator = "domain_management.user_already_moderator";
    public const string AlreadySuspended = "domain_management.user_already_suspended";
    public const string NotSuspended = "domain_management.user_not_suspended";
    public const string AlreadyEnabled = "domain_management.already_enabled";
    public const string AlreadyDisabled = "domain_management.already_disabled";
    public const string AlreadyVerified = "domain_already_verified";
    public const string VerificationFailed = "domain_verification_failed";
    public const string UserNotModerator = "domain_management.user_not_moderator";
    public const string UserNotViewer = "domain_management.user_not_viewer";
    public const string UserAlreadyViewer = "domain_management.user_already_viewer";
}