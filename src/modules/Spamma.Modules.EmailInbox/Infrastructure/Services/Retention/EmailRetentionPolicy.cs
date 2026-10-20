using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;

public sealed record EmailRetentionPolicy(string Id, EmailRetentionMode Mode, int? MaxAgeHours, int? MaxMessages)
{
    public static string DomainKey(Guid domainId) => $"retention:domain:{domainId:N}";

    public static string SubdomainKey(Guid subdomainId) => $"retention:subdomain:{subdomainId:N}";
}
