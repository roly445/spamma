using MailKit.Net.Smtp;
using MimeKit;

// Simple bulk email sender for Spamma testing
// Usage: dotnet run -- --host localhost --port 20 --from sender@example.com --to recipient@spamma.io --batch 50 --batches 1

var parser = new SimpleArgs(args);
var host = parser.Get("host") ?? "localhost";
var port = int.TryParse(parser.Get("port"), out var p) ? p : 25;
var from = parser.Get("from") ?? "tester@localhost";
var to = parser.Get("to") ?? "test@mail.spamma.dev";
var batchSize = int.TryParse(parser.Get("batch"), out var b) ? Math.Clamp(b, 1, 100) : 100;
var batches = int.TryParse(parser.Get("batches"), out var bs) ? Math.Max(1, bs) : 1;
var subject = parser.Get("subject") ?? "Spamma Load Test";
var htmlBody = parser.Get("html") ?? "<p>This is the <strong>HTML</strong> part</p>";
var textBody = parser.Get("text") ?? "This is the text part";

Console.WriteLine($"Host={host}:{port} From={from} To={to} BatchSize={batchSize} Batches={batches}");

for (var batchIndex = 0; batchIndex < batches; batchIndex++)
{
    var batchId = Guid.NewGuid();

    var tasks = new List<Task>();
    for (var i = 0; i < batchSize; i++)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject + " " + (i + 1);
        message.Headers.Add("X-Spamma-Camp", batchId.ToString());

        var builder = new BodyBuilder
        {
            TextBody = textBody + " - " + (i + 1),
            HtmlBody = htmlBody + " - " + (i + 1)
        };
        message.Body = builder.ToMessageBody();

        tasks.Add(SendAsync(host, port, message));
    }
    Console.WriteLine($"Sending batch {batchIndex + 1}/{batches} with X-Spamma-Comp: {batchId}");
    await Task.WhenAll(tasks);
    Console.WriteLine($"Batch {batchIndex + 1} complete.");
}

Console.WriteLine("All batches sent.");

async Task SendAsync(string host, int port, MimeMessage message)
{
    try
    {
        using var client = new SmtpClient();
        client.Timeout = 10000; // 10s
        // Use no SSL for localhost testing on port 20
        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.None);
        // No auth assumed for local capture
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        Console.WriteLine($"Sent: {message.Subject}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send {message.Subject}: {ex.Message}");
    }
}

// Minimal argument parser
class SimpleArgs
{
    private readonly Dictionary<string, string?> _map = new();
    public SimpleArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--"))
            {
                var key = a[2..];
                string? val = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    val = args[i + 1];
                    i++;
                }
                _map[key] = val;
            }
        }
    }

    public string? Get(string key) => _map.TryGetValue(key, out var v) ? v : null;
}

