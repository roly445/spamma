using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Spamma.Tools.EmailLoadTester;

// Simple bulk email sender for Spamma testing
// Usage: dotnet run -- --host localhost --port 25 --from sender@example.com --to recipient@spamma.io --batch 50 --batches 1
var parser = new SimpleArgs(args);
var host = parser.Get("host") ?? "localhost";
var port = int.TryParse(parser.Get("port"), out var p) ? p : 25;
var from = parser.Get("from") ?? "tester@localhost";
var to = parser.Get("to") ?? "test@mail.spamma.dev";
var batchSize = int.TryParse(parser.Get("batch"), out var b) ? Math.Clamp(b, 1, 100) : 1;
var batches = int.TryParse(parser.Get("batches"), out var bs) ? Math.Max(1, bs) : 1;
var subject = parser.Get("subject") ?? "Spamma Load Test";
var htmlBody = parser.Get("html") ?? "<p>This is the <strong>HTML</strong> part</p>";
var textBody = parser.Get("text") ?? "This is the text part";
var campaign = parser.Get("campaign");
var tlsMode = parser.Get("tls") ?? "none";
var timeoutMs = int.TryParse(parser.Get("timeout"), out var timeout) ? Math.Max(1000, timeout) : 10000;
var allowInvalidCertificate = ParseBool(parser.Get("allow-invalid-cert"));
var secureSocketOptions = ParseSecureSocketOptions(tlsMode);

Console.WriteLine($"Host={host}:{port} TLS={secureSocketOptions} AllowInvalidCert={allowInvalidCertificate} TimeoutMs={timeoutMs} From={from} To={to} BatchSize={batchSize} Batches={batches} Campaign={campaign ?? "(none)"}");

var totalSent = 0;
var totalFailed = 0;

for (var batchIndex = 0; batchIndex < batches; batchIndex++)
{
    var tasks = new List<Task<bool>>();
    for (var i = 0; i < batchSize; i++)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject + " " + (i + 1);

        if (!string.IsNullOrWhiteSpace(campaign))
        {
            message.Headers.Add("x-spamma-camp", campaign);
        }

        var builder = new BodyBuilder
        {
            TextBody = textBody + " - " + (i + 1),
            HtmlBody = htmlBody + " - " + (i + 1),
        };
        message.Body = builder.ToMessageBody();

        tasks.Add(SendAsync(host, port, secureSocketOptions, timeoutMs, allowInvalidCertificate, message));

        // Add random delay between 0 and 100ms to mimic real SMTP server timing
        // Don't delay after last email
        if (i < batchSize - 1)
        {
            var delayMs = Random.Shared.Next(0, 1001); // 0-1000ms (1 second)
            await Task.Delay(delayMs);
        }
    }

    Console.WriteLine($"Sending batch {batchIndex + 1}/{batches} with x-spamma-camp: {campaign ?? "(none)"}");
    var results = await Task.WhenAll(tasks);
    var sent = results.Count(success => success);
    var failed = results.Length - sent;
    totalSent += sent;
    totalFailed += failed;
    Console.WriteLine($"Batch {batchIndex + 1} complete. Sent={sent} Failed={failed}");
}

Console.WriteLine($"All batches complete. Sent={totalSent} Failed={totalFailed}");
if (totalFailed > 0)
{
    Environment.ExitCode = 1;
}

async Task<bool> SendAsync(string host, int port, SecureSocketOptions secureSocketOptions, int timeoutMs, bool allowInvalidCertificate, MimeMessage message)
{
    try
    {
        using var client = new SmtpClient();
        client.Timeout = timeoutMs;
        if (allowInvalidCertificate)
        {
#pragma warning disable S4830
            // Test-only switch for probing hosted SMTP endpoints with expired/self-signed certificates.
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore S4830
        }

        Console.WriteLine($"Connecting: {host}:{port} TLS={secureSocketOptions} Subject=\"{message.Subject}\"");
        await client.ConnectAsync(host, port, secureSocketOptions);
        Console.WriteLine($"Connected: {host}:{port} TLSActive={client.IsSecure} Capabilities={client.Capabilities}");

        Console.WriteLine($"Sending: {message.Subject}");
        await client.SendAsync(message);
        Console.WriteLine($"Disconnecting: {message.Subject}");
        await client.DisconnectAsync(true);
        Console.WriteLine($"Sent: {message.Subject}");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send {message.Subject}: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine(ex);
        return false;
    }
}

static bool ParseBool(string? value)
{
    return value?.Trim().ToLowerInvariant() is "true" or "yes" or "1" or "on";
}

static SecureSocketOptions ParseSecureSocketOptions(string tlsMode)
{
    return tlsMode.Trim().ToLowerInvariant() switch
    {
        "none" => SecureSocketOptions.None,
        "off" => SecureSocketOptions.None,
        "false" => SecureSocketOptions.None,
        "starttls" => SecureSocketOptions.StartTls,
        "starttls-when-available" => SecureSocketOptions.StartTlsWhenAvailable,
        "starttlswhenavailable" => SecureSocketOptions.StartTlsWhenAvailable,
        "auto" => SecureSocketOptions.Auto,
        "ssl" => SecureSocketOptions.SslOnConnect,
        "ssl-on-connect" => SecureSocketOptions.SslOnConnect,
        "sslonconnect" => SecureSocketOptions.SslOnConnect,
        _ => throw new ArgumentException(
            $"Unsupported --tls value '{tlsMode}'. Use none, starttls, starttls-when-available, auto, or ssl.",
            nameof(tlsMode)),
    };
}
