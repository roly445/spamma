using System.Collections.Immutable;
using System.Net;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.IntegrationEvents;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;

namespace Spamma.Modules.UserManagement.Application.IntegrationEventSubscribers;

public class SendWelcomeEmailToNewUsers(
    ILogger<SendWelcomeEmailToNewUsers> logger, IEmailSender emailSender, IOptions<Settings> settings)
    : ICapSubscribe
{
    [CapSubscribe(IntegrationEventNames.UserCreated)]
    public Task Process(UserCreatedIntegrationEvent ev)
    {
        if (!ev.SendWelcome)
        {
            logger.LogInformation("Skipping welcome email for user {UserId} as SendWelcome is false", ev.UserId);
            return Task.CompletedTask;
        }

        var emailBody = new List<Tuple<EmailTemplateSection, ImmutableArray<string>>>
        {
            new(EmailTemplateSection.Text, [$"Dear {WebUtility.HtmlEncode(ev.Name)},"]),
            new(EmailTemplateSection.Text, ["You have been added to the Spamma platform. We are pleased to welcome you as a new user."]),
            new(EmailTemplateSection.Text, [$"To access the platform, please visit: {settings.Value.BaseUri}"]),
            new(EmailTemplateSection.Text, ["Best regards,"]),
            new(EmailTemplateSection.Text, ["The Spamma Team"]),
        };
        return emailSender.SendEmailAsync(ev.Name, ev.EmailAddress, "Register", emailBody);
    }
}