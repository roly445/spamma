using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal record DnsSetupResult(DnsSetupStatus Status, string Message);
