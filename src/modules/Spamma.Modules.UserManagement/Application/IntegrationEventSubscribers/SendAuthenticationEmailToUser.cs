using System.Collections.Immutable;
using System.Net;
using DotNetCore.CAP;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

public class SendAuthenticationEmailToUser(IAuthTokenProvider authTokenProvider, IEmailSender emailSender, IOptions<Settings> settings)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.AuthenticationStarted)]
    public async Task Process(AuthenticationStartedIntegrationEvent ev)
    {
        var token = authTokenProvider.GenerateAuthenticationToken(new IAuthTokenProvider.AuthenticationTokenModel(ev.UserId, ev.SecurityStamp, ev.WhenHappened,
            ev.AuthenticationAttemptId));

        if (token.IsFailure)
        {
            return;
        }

        var encodedString = WebUtility.UrlEncode(token.Value);
        var emailBody = new List<Tuple<EmailTemplateSection, ImmutableArray<string>>>
        {
            new(EmailTemplateSection.Text,  [
                $"Hi {ev.Name},",
                "Click the link below to sign in to Spamma:"
            ]),
            new(
                EmailTemplateSection.ActionLink,
                [
                    string.Format(settings.Value.LoginUri, encodedString),
                    "Sign in to Spamma"
                ]),
            new(EmailTemplateSection.Text,  [
                $"If the link does not open, copy and paste the following URL into your browser:",
                "Click the link below to sign in to Spamma:",
                string.Format(settings.Value.LoginUri, encodedString),
                "If you did not request this, you can safely ignore this message."
            ]),
        };
        await emailSender.SendEmailAsync(ev.Name, ev.EmailAddress, "Authenticate your Spamma account", emailBody);
    }
}