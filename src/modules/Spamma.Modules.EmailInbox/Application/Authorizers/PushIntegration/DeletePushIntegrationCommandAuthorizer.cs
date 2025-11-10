using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.PushIntegration;

/// <summary>
/// Authorizer for DeletePushIntegrationCommand.
/// </summary>
public class DeletePushIntegrationCommandAuthorizer : AbstractRequestAuthorizer<DeletePushIntegrationCommand>
{
    /// <inheritdoc />
    public override void BuildPolicy(DeletePushIntegrationCommand request)
    {
        // Ownership check is performed in the handler
    }
}