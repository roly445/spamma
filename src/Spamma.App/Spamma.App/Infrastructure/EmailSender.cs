using System.Collections.Immutable;
using System.Reflection;
using FluentEmail.Core;
using ResultMonad;
using Spamma.Modules.Common;

namespace Spamma.App.Infrastructure;

public class EmailSender(IFluentEmail fluentEmail) : IEmailSender
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
        var email = fluentEmail.To(emailAddress, name)
            .Subject(subject)
            .UsingTemplateFromEmbedded(
                "Spamma.App.Infrastructure.Template.html",
                new EmailModel(body.Select(x => string.Format(EmailTemplateSections[x.Item1], x.Item2.ToArray<object?>())).ToList()),
                typeof(EmailSender).GetTypeInfo().Assembly);
        email.Data.Tags = new List<string>
        {
            "Spamma",
        };

        var sendResponse = await email.SendAsync(cancellationToken);
        return sendResponse.Successful ? Result.Ok() : Result.Fail();
    }

    public sealed record EmailModel(IReadOnlyList<string> BodyContent);
}