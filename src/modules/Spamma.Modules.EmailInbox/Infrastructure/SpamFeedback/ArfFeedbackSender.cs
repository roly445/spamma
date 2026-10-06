using System.Globalization;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed class ArfFeedbackSender(IArfSmtpSettingsProvider settingsProvider) : IArfFeedbackSender
{
    public static MimeMessage BuildReport(SpamReport report, MimeMessage original, string fromEmail, string fromName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(report.ArfRecipient ?? throw new InvalidOperationException("No ARF destination is configured.")));
        message.Subject = $"Spam report: {original.Subject}";
        message.Headers.Add("X-Spamma-Report-Id", report.Id.ToString());

        var feedback = new StringBuilder()
            .Append("Feedback-Type: abuse\r\n")
            .Append("User-Agent: Spamma/1.0\r\n")
            .Append("Version: 1\r\n");
        if (MailboxAddress.TryParse(report.Sender, out var sender))
        {
            feedback.Append("Original-Mail-From: <").Append(sender.Address).Append(">\r\n");
        }

        if (MailboxAddress.TryParse(report.Recipient, out var recipient))
        {
            feedback.Append("Original-Rcpt-To: <").Append(recipient.Address).Append(">\r\n");
        }

        feedback.Append("Arrival-Date: ")
            .Append(report.CreatedAt.ToString("ddd, dd MMM yyyy HH:mm:ss ", CultureInfo.InvariantCulture))
            .Append(report.CreatedAt.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", string.Empty, StringComparison.Ordinal))
            .Append("\r\n")
            .Append("X-Spamma-Report-Id: ").Append(report.Id).Append("\r\n");

        var feedbackBytes = Encoding.ASCII.GetBytes(feedback.ToString());
        var reportBody = new MultipartReport("feedback-report")
        {
            new TextPart("plain") { Text = $"Spamma test complaint for captured message {report.Id}." },
            new MimePart("message", "feedback-report")
            {
                Content = new MimeContent(new MemoryStream(feedbackBytes, writable: false)),
                ContentTransferEncoding = ContentEncoding.SevenBit,
            },
            new MessagePart { Message = original },
        };
        message.Body = reportBody;
        return message;
    }

    public async Task SendAsync(SpamReport report, MimeMessage original, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(report.ArfRecipient))
        {
            throw new InvalidOperationException("No ARF destination is configured.");
        }

        var settings = await settingsProvider.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException("Outbound SMTP settings are incomplete.");
        }

        var message = BuildReport(report, original, settings.FromEmail, settings.FromName);
        using var smtp = new SmtpClient();
        var security = SecureSocketOptions.None;
        if (settings.UseTls)
        {
            security = settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        }

        await smtp.ConnectAsync(settings.Host, settings.Port, security, cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await smtp.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
