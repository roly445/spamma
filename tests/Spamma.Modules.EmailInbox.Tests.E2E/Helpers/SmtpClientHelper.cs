using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Helpers;

/// <summary>
/// Helper class for sending test emails via SMTP using MailKit.
/// </summary>
public class SmtpClientHelper
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;

    public SmtpClientHelper(string smtpHost, int smtpPort)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
    }

    /// <summary>
    /// Sends an email via SMTP and returns the server response.
    /// </summary>
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
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.None, cancellationToken);

        // Send message
        var response = await client.SendAsync(message, cancellationToken);

        // Disconnect
        await client.DisconnectAsync(true, cancellationToken);

        return response;
    }

    /// <summary>
    /// Sends an email with multiple recipients.
    /// </summary>
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
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.None, cancellationToken);
        var response = await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return response;
    }

    /// <summary>
    /// Attempts to send an email and captures any SMTP exception.
    /// Returns tuple: (success, response/error message).
    /// </summary>
    public async Task<(bool Success, string Message)> TrySendEmailAsync(
        string from,
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendEmailAsync(from, to, subject, body, cancellationToken: cancellationToken);
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
