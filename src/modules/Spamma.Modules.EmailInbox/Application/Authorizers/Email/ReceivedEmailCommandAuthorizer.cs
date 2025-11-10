using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Email;

/// <summary>
/// Authorizer for ReceivedEmailCommand.
/// This is an internal system command that doesn't require user authentication.
/// </summary>
public class ReceivedEmailCommandAuthorizer : AbstractRequestAuthorizer<ReceivedEmailCommand>
{
    public override void BuildPolicy(ReceivedEmailCommand request)
    {
        // No authorization requirements - this is an internal system command
        // triggered by SMTP message processing
    }
}
