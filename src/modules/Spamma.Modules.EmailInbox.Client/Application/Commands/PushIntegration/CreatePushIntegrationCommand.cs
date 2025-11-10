using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.EmailInbox.Client.Application;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

/// <summary>
/// Command to create a push integration.
/// </summary>
[BluQubeCommand(Path = "api/email-push/integrations")]
public record CreatePushIntegrationCommand(
    Guid SubdomainId,
    string? Name,
    string EndpointUrl,
    FilterType FilterType,
    string? FilterValue) : ICommand;