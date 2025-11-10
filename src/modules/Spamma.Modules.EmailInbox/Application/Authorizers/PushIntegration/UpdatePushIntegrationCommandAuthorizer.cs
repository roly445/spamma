using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.PushIntegration;

/// <summary>
/// Authorizer for UpdatePushIntegrationCommand.
/// </summary>
public class UpdatePushIntegrationCommandAuthorizer : AbstractRequestAuthorizer<UpdatePushIntegrationCommand>
{
    /// <inheritdoc />
    public override void BuildPolicy(UpdatePushIntegrationCommand request)
    {
        // Ownership check is performed in the handler
    }
}