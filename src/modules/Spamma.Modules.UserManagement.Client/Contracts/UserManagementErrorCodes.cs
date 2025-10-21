namespace Spamma.Modules.UserManagement.Client.Contracts;

public static class UserManagementErrorCodes
{
    public const string AccountSuspended = "user_management.account_suspended";
    public const string InvalidAuthenticationAttempt = "user_management.invalid_authentication_attempt";
    public const string NotSuspended = "user_management.not_suspended";
    public const string AlreadySuspended = "user_management.already_suspended";
}