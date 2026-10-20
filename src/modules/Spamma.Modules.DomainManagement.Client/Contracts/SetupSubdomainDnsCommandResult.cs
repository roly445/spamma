using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Contracts;

public record SetupSubdomainDnsCommandResult(DnsSetupStatus Status, string Message) : ICommandResult;
