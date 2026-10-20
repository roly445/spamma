using Marten;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;

public sealed class EmailRetentionPolicyService(IDocumentSession session)
{
    public static readonly EmailRetentionPolicy Default = new("default", EmailRetentionMode.Override, 24, null);

    public async Task<EmailRetentionPolicy> GetDomainAsync(Guid domainId, CancellationToken cancellationToken) =>
        await session.LoadAsync<EmailRetentionPolicy>(EmailRetentionPolicy.DomainKey(domainId), cancellationToken) ?? Default;

    public async Task<EmailRetentionPolicy> GetSubdomainAsync(Guid subdomainId, CancellationToken cancellationToken) =>
        await session.LoadAsync<EmailRetentionPolicy>(EmailRetentionPolicy.SubdomainKey(subdomainId), cancellationToken)
        ?? new EmailRetentionPolicy(EmailRetentionPolicy.SubdomainKey(subdomainId), EmailRetentionMode.Inherit, null, null);

    public async Task<EmailRetentionPolicy> GetEffectiveAsync(Guid domainId, Guid subdomainId, CancellationToken cancellationToken)
    {
        var subdomain = await this.GetSubdomainAsync(subdomainId, cancellationToken);
        return subdomain.Mode == EmailRetentionMode.Override
            ? subdomain
            : await this.GetDomainAsync(domainId, cancellationToken);
    }

    public async Task<EmailRetentionPolicy> SetDomainAsync(
        Guid domainId, int? maxAgeHours, int? maxMessages, CancellationToken cancellationToken)
    {
        var policy = new EmailRetentionPolicy(EmailRetentionPolicy.DomainKey(domainId), EmailRetentionMode.Override, maxAgeHours, maxMessages);
        session.Store(policy);
        await session.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<EmailRetentionPolicy> SetSubdomainAsync(
        Guid subdomainId, EmailRetentionMode mode, int? maxAgeHours, int? maxMessages, CancellationToken cancellationToken)
    {
        var policy = new EmailRetentionPolicy(EmailRetentionPolicy.SubdomainKey(subdomainId), mode,
            mode == EmailRetentionMode.Override ? maxAgeHours : null,
            mode == EmailRetentionMode.Override ? maxMessages : null);
        session.Store(policy);
        await session.SaveChangesAsync(cancellationToken);
        return policy;
    }
}
