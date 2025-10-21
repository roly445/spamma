using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/check-mx")]
public record CheckMxRecordCommand(Guid SubdomainId) : ICommand<CheckMxRecordCommandResult>;