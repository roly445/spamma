using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Helpers;

public class SmtpClientHelper
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;

    public SmtpClientHelper(string smtpHost, int smtpPort)
    {
        this._smtpHost = smtpHost;
        this._smtpPort = smtpPort;
    }

    public async Task<string> SendEmailAsync(
        string from,
        string to,
        string subject,
        string body,
        Dictionary<string, string>? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage
        {
            From = { MailboxAddress.Parse(from) },
            To = { MailboxAddress.Parse(to) },
            Subject = subject,
            Body = new TextPart("plain") { Text = body },
        };

        // Add custom headers if provided
        if (customHeaders != null)
        {
            foreach (var (key, value) in customHeaders)
            {
            message.Headers.Add(key, value);
            }
        }

        using var client = new SmtpClient();

        // Connect to SMTP server without TLS (test environment)
        await client.ConnectAsync(this._smtpHost, this._smtpPort, SecureSocketOptions.None, cancellationToken);

        // Send message
        var response = await client.SendAsync(message, cancellationToken);

        // Disconnect
        await client.DisconnectAsync(true, cancellationToken);

        return response;
    }

    public async Task<string> SendEmailWithMultipleRecipientsAsync(
        string from,
        IEnumerable<string> toAddresses,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage
        {
            From = { MailboxAddress.Parse(from) },
            Subject = subject,
            Body = new TextPart("plain") { Text = body },
        };

        foreach (var address in toAddresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(this._smtpHost, this._smtpPort, SecureSocketOptions.None, cancellationToken);
        var response = await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return response;
    }

    public async Task<(bool Success, string Message)> TrySendEmailAsync(
        string from,
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this.SendEmailAsync(from, to, subject, body, cancellationToken: cancellationToken);
            return (true, response);
        }
        catch (SmtpCommandException ex)
        {
            // SMTP server rejected the message (e.g., 550, 450, 553)
            return (false, $"{(int)ex.StatusCode} {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}