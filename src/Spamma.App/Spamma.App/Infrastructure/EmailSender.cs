using System.Collections.Immutable;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.Extensions.Logging;
using ResultMonad;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common;

namespace Spamma.App.Infrastructure;

public class EmailSender(
    IFluentEmail fluentEmail,
    IAppConfigurationService appConfigurationService,
    IConfiguration configuration,
    ILogger<EmailSender> logger) : IEmailSender
{
    private static readonly IReadOnlyDictionary<EmailTemplateSection, string> EmailTemplateSections = new Dictionary<EmailTemplateSection, string>
    {
        {
            EmailTemplateSection.Text,
            @"<tr style=""font-family: 'Helvetica Neue',Helvetica,Arial,sans-serif; box-sizing: border-box; font-size: 14px; margin: 0;"">
    <td class=""content-block"" style=""font-family: 'Helvetica Neue',Helvetica,Arial,sans-serif; box-sizing: border-box; font-size: 14px; vertical-align: top; margin: 0; padding: 0 0 20px;"" valign=""top"">
        {0}
    </td>
</tr>"
        },
        {
            EmailTemplateSection.ActionLink,
            @"<tr style=""font-family: 'Helvetica Neue',Helvetica,Arial,sans-serif; box-sizing: border-box; font-size: 14px; margin: 0;"">
    <td class=""content-block"" itemprop=""handler"" itemscope itemtype=""http://schema.org/HttpActionHandler"" style=""font-family: 'Helvetica Neue',Helvetica,Arial,sans-serif; box-sizing: border-box; font-size: 14px; vertical-align: top; margin: 0; padding: 0 0 20px;"" valign=""top"">
        <a href=""{0}"" class=""btn-primary"" itemprop=""url"" style=""font-family: 'Helvetica Neue',Helvetica,Arial,sans-serif; box-sizing: border-box; font-size: 14px; color: #FFF; text-decoration: none; line-height: 2em; font-weight: bold; text-align: center; cursor: pointer; display: inline-block; border-radius: 5px; text-transform: capitalize; background-color: #348eda; margin: 0; border-color: #348eda; border-style: solid; border-width: 10px 20px;"">{1}</a>
    </td>
</tr>"
        },
    };

    public async Task<Result> SendEmailAsync(string name, string emailAddress, string subject,
        List<Tuple<EmailTemplateSection, ImmutableArray<string>>> body, CancellationToken cancellationToken = default)
    {
        var savedSettings = await appConfigurationService.GetEmailSettingsAsync();
        var hasSavedSettings = !string.IsNullOrWhiteSpace(savedSettings.SmtpHost);
        var smtpHost = hasSavedSettings ? savedSettings.SmtpHost : configuration["Settings:EmailSmtpHost"] ?? "localhost";
        var fallbackPort = int.TryParse(configuration["Settings:EmailSmtpPort"], out var port) ? port : 587;
        var smtpPort = hasSavedSettings ? savedSettings.SmtpPort : fallbackPort;
        var fromEmail = hasSavedSettings ? savedSettings.FromEmail : configuration["Settings:FromEmailAddress"] ?? "noreply@example.com";
        var fromName = hasSavedSettings ? savedSettings.FromName : configuration["Settings:FromName"] ?? "Spamma";
        var username = hasSavedSettings ? savedSettings.Username : configuration["Settings:EmailSmtpUsername"];
        var password = hasSavedSettings ? savedSettings.Password : configuration["Settings:EmailSmtpPassword"];
        var useTls = hasSavedSettings ? savedSettings.UseTls : bool.TryParse(configuration["Settings:EmailSmtpUseTls"], out var tls) && tls;

        var email = fluentEmail.To(emailAddress, name)
            .SetFrom(fromEmail, fromName)
            .Subject(subject)
            .UsingTemplateFromEmbedded(
                "Spamma.App.Infrastructure.Template.html",
                new EmailModel(body.Select(x => string.Format(EmailTemplateSections[x.Item1], x.Item2.ToArray<object?>())).ToList()),
                typeof(EmailSender).GetTypeInfo().Assembly);
        email.Data.Tags = new List<string>
        {
            "Spamma",
        };

        logger.LogInformation("Sending email to {EmailAddress} with subject {Subject}", emailAddress, subject);

        var sender = new SmtpSender(() =>
        {
#pragma warning disable S5332 // TLS follows the operator's saved SMTP setting; local test SMTP intentionally has no TLS.
            var client = new SmtpClient(smtpHost, smtpPort) { EnableSsl = useTls };
#pragma warning restore S5332
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password ?? string.Empty);
            }

            return client;
        });
        var sendResponse = await sender.SendAsync(email, cancellationToken);

        if (sendResponse.Successful)
        {
            logger.LogInformation("Email sent successfully to {EmailAddress}", emailAddress);
            return Result.Ok();
        }

        logger.LogError(
            "Failed to send email to {EmailAddress} with subject {Subject}. Errors: {Errors}",
            emailAddress,
            subject,
            string.Join("; ", sendResponse.ErrorMessages));
        return Result.Fail();
    }

    public sealed record EmailModel(IReadOnlyList<string> BodyContent);
}
