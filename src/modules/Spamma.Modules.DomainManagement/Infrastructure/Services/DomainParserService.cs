using Nager.PublicSuffix;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services;

public class DomainParserService : IDomainParserService
{
    private IDomainParser? _domainParser;

    public void SetDomainParser(IDomainParser domainParser)
    {
        this._domainParser = domainParser ?? throw new ArgumentNullException(nameof(domainParser));
    }

    public DomainInfo? Parse(string domain)
    {
        if (this._domainParser == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        return this._domainParser.Parse(domain);
    }

    public bool IsValidDomain(string domain)
    {
        try
        {
            if (this._domainParser == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                return false;
            }

            var domainInfo = this._domainParser.Parse(domain);
            return domainInfo != null && !string.IsNullOrEmpty(domainInfo.RegistrableDomain);
        }
        catch
        {
            return false;
        }
    }
}