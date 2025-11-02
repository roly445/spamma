using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

[BluQubeCommand(Path = "api/subdomain-management/suspend")]
public record SuspendSubdomainCommand(Guid SubdomainId, SubdomainSuspensionReason Reason, string? Notes) : ICommand;