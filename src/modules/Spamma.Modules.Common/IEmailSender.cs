using System.Collections.Immutable;
using ResultMonad;

namespace Spamma.Modules.Common;

public enum EmailTemplateSection
{
    Text,
    ActionLink,
}

public interface IEmailSender
{
    Task<Result> SendEmailAsync(string name, string emailAddress, string subject,
        List<Tuple<EmailTemplateSection, ImmutableArray<string>>> body, CancellationToken cancellationToken = default);
}