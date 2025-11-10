using BluQube.Attributes;
using BluQube.Commands;
using Spamma.Modules.EmailInbox.Client.Application;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

/// <summary>
/// Command to update a push integration.
/// </summary>
[BluQubeCommand(Path = "api/email-push/integrations/{integrationId}")]
public record UpdatePushIntegrationCommand(
    Guid IntegrationId,
    string? Name,
    string EndpointUrl,
    FilterType FilterType,
    string? FilterValue) : ICommand;