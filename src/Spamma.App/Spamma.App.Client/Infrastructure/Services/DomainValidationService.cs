using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Infrastructure.Services;

public class DomainValidationService : IDomainValidationService
{
    private SimpleHttpRuleProvider? ruleProvider;

    public Task BuildDomainListAsync(CancellationToken cancellationToken = default)
    {
        this.ruleProvider = new SimpleHttpRuleProvider();
        return this.ruleProvider.BuildAsync(cancellationToken: cancellationToken);
    }

    public bool IsDomainValid(string domain)
    {
        if (this.ruleProvider == null)
        {
            throw new InvalidOperationException("Domain list not built. Call BuildDomainListAsync first.");
        }

        var parser = new DomainParser(this.ruleProvider);
        var info = parser.Parse(domain);

        if (info == null)
        {
            return false;
        }

        return info.RegistrableDomain != null &&
               info.RegistrableDomain.Equals(domain, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSubdomainValid(string domain)
    {
        throw new NotImplementedException();
    }
}