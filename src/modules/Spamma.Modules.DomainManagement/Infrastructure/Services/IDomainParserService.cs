using Nager.PublicSuffix;

namespace Spamma.Modules.DomainManagement.Infrastructure.Services;

public interface IDomainParserService
{
    DomainInfo? Parse(string domain);

    bool IsValidDomain(string domain);
}
