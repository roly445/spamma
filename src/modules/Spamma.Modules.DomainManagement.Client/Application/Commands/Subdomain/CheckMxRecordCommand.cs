using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

[BluQubeCommand(Path = "api/subdomain-management/check-mx")]
public record CheckMxRecordCommand(Guid SubdomainId) : ICommand<CheckMxRecordCommandResult>;