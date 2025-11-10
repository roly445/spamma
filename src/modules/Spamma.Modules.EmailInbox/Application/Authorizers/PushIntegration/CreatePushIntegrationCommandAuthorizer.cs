using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.PushIntegration;

/// <summary>
/// Authorizer for CreatePushIntegrationCommand.
/// </summary>
public class CreatePushIntegrationCommandAuthorizer : AbstractRequestAuthorizer<CreatePushIntegrationCommand>
{
    /// <inheritdoc />
    public override void BuildPolicy(CreatePushIntegrationCommand request)
    {
        // Authorization bypassed for now
    }
}