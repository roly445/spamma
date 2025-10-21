namespace Spamma.Modules.Common.IntegrationEvents;

public static class IntegrationEventNames
{
    public const string UserCreated = "user-management.user.user-created";
    public const string AuthenticationStarted = "user-management.authentication.authentication-started";
    public const string UserSuspended = "user-management.user.user-suspended";
    public const string UserUnsuspended = "user-management.user.user-unsuspended";
    public const string EmailDeleted = "email-inbox.email.email-deleted";
}