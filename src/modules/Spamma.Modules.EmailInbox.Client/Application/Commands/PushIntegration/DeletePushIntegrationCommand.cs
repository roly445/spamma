using BluQube.Attributes;
using BluQube.Commands;

namespace Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

/// <summary>
/// Command to delete a push integration.
/// </summary>
[BluQubeCommand(Path = "api/email-push/integrations/{integrationId}")]
public record DeletePushIntegrationCommand(Guid IntegrationId) : ICommand;