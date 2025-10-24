namespace Spamma.Modules.Common.IntegrationEvents;

public static class IntegrationEventNames
{
    public const string UserCreated = "user-management.user.user-created";
    public const string AuthenticationStarted = "user-management.authentication.authentication-started";
    public const string UserSuspended = "user-management.user.user-suspended";
    public const string UserUnsuspended = "user-management.user.user-unsuspended";
    public const string EmailDeleted = "email-inbox.email.email-deleted";
    public const string EmailReceived = "email-inbox.email.email-received";
    public const string UserAddedAsDomainModerator = "domain-management.domain.user-added-as-moderator";
    public const string UserAddedAsSubdomainModerator = "domain-management.subdomain.user-added-as-moderator";
    public const string UserAddedAsSubdomainViewer = "domain-management.subdomain.user-added-as-viewer";
    public const string UserRemovedFromBeingDomainModerator = "domain-management.domain.user-removed-as-moderator";
    public const string UserRemovedFromBeingSubdomainModerator = "domain-management.subdomain.user-removed-as-moderator";
    public const string UserRemovedFromBeingSubdomainViewer = "domain-management.subdomain.user-removed-as-viewer";
    public const string UserDetailsUpdated = "user-management.user.user-details-updated";
}