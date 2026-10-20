using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Contracts;

public record SetupDomainDnsCommandResult(DnsSetupStatus Status, string Message) : ICommandResult;
