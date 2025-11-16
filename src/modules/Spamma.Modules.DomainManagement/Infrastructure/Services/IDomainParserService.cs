using Nager.PublicSuffix;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services;

public interface IDomainParserService
{
    void SetDomainParser(IDomainParser domainParser);

    DomainInfo? Parse(string domain);

    bool IsValidDomain(string domain);
}