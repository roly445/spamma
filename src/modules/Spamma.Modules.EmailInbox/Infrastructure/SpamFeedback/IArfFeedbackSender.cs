using MimeKit;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public interface IArfFeedbackSender
{
    Task SendAsync(SpamReport report, MimeMessage original, CancellationToken cancellationToken);
}
