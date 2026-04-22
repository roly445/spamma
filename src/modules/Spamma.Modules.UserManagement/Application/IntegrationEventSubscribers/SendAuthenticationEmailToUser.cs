using System.Collections.Immutable;
using System.Net;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

public class SendAuthenticationEmailToUser(
    ILogger<SendAuthenticationEmailToUser> logger,
    IAuthTokenProvider authTokenProvider,
    IEmailSender emailSender,
    IOptions<Settings> settings)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.AuthenticationStarted)]
    public async Task Process(AuthenticationStartedIntegrationEvent ev)
    {
        logger.LogInformation(
            "Processing AuthenticationStarted event for {UserId} ({EmailAddress}), attempt {AttemptId}",
            ev.UserId,
            ev.EmailAddress,
            ev.AuthenticationAttemptId);

        var token = authTokenProvider.GenerateAuthenticationToken(new IAuthTokenProvider.AuthenticationTokenModel(ev.UserId, ev.SecurityStamp, ev.WhenHappened,
            ev.AuthenticationAttemptId));

        if (token.IsFailure)
        {
            logger.LogError(
                "Failed to generate authentication token for {UserId} ({EmailAddress}), attempt {AttemptId}",
                ev.UserId,
                ev.EmailAddress,
                ev.AuthenticationAttemptId);
            return;
        }

        logger.LogDebug("Authentication token generated for {UserId}, attempt {AttemptId}", ev.UserId, ev.AuthenticationAttemptId);

        var encodedString = WebUtility.UrlEncode(token.Value);
        var loginUri = string.Format(settings.Value.LoginUri, encodedString);

        var emailBody = new List<Tuple<EmailTemplateSection, ImmutableArray<string>>>
        {
            new(EmailTemplateSection.Text, [$"Hi {ev.Name},"]),
            new(EmailTemplateSection.Text, ["Click the link below to sign in to Spamma:"]),
            new(
                EmailTemplateSection.ActionLink,
                [
                    loginUri,
                    "Sign in to Spamma"
                ]),
            new(EmailTemplateSection.Text, ["If the link does not open, copy and paste the following URL into your browser:"]),
            new(EmailTemplateSection.Text, ["Click the link below to sign in to Spamma:"]),
            new(EmailTemplateSection.Text, [loginUri]),
            new(EmailTemplateSection.Text, ["If you did not request this, you can safely ignore this message."]),
        };

        logger.LogInformation(
            "Sending magic link email to {EmailAddress} for {UserId}, attempt {AttemptId}",
            ev.EmailAddress,
            ev.UserId,
            ev.AuthenticationAttemptId);

        try
        {
            var result = await emailSender.SendEmailAsync(ev.Name, ev.EmailAddress, "Authenticate your Spamma account", emailBody);
            if (result.IsFailure)
            {
                logger.LogError(
                    "Magic link email failed to send to {EmailAddress} for {UserId}, attempt {AttemptId}",
                    ev.EmailAddress,
                    ev.UserId,
                    ev.AuthenticationAttemptId);
                return;
            }

            logger.LogInformation(
                "Magic link email sent successfully to {EmailAddress} for {UserId}, attempt {AttemptId}",
                ev.EmailAddress,
                ev.UserId,
                ev.AuthenticationAttemptId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception sending magic link email to {EmailAddress} for {UserId}, attempt {AttemptId}. SMTP host may be unreachable.",
                ev.EmailAddress,
                ev.UserId,
                ev.AuthenticationAttemptId);
        }
    }
}
