using System.Text.Json;
using Grpc.Net.Client;
using MimeKit;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;

namespace Spamma.Samples.GrpcEmailPushClient;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // CLI parsing: --server <url> --api-key <key> --insecure
        var serverUrl = Environment.GetEnvironmentVariable("SPAMMA_SERVER_URL") ?? GetDefaultServerUrl();
        var apiKey = string.Empty;
        var apiKeyFile = string.Empty;
        var jsonOutput = false;
        var serverPath = string.Empty; // optional path append, e.g., /api/grpc
        var verbose = false;

        // Service method path (unchanged for sample client). Declared here so it's available to logging
        var serviceMethodPath = "/spamma.email_push.EmailPushService/SubscribeToEmails";
        var i = 0;
        while (i < args.Length)
        {
            var a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    Console.WriteLine("Usage: Spamma.Samples.GrpcEmailPushClient --server <url> --api-key <key> [--insecure] [--path <serverPath>] [--json] [--verbose]");
                    return 0;
                case "-s":
                case "--server":
                    if (i + 1 < args.Length)
                    {
                        serverUrl = args[i + 1];
                        i++;
                    }

                    break;
                case "-k":
                case "--api-key":
                case "--key":
                    if (i + 1 < args.Length)
                    {
                        apiKey = args[i + 1];
                        i++;
                    }

                    break;
                case "-f":
                case "--api-key-file":
                case "--key-file":
                    if (i + 1 < args.Length)
                    {
                        apiKeyFile = args[i + 1];
                        i++;
                    }

                    break;
                case "--json":
                    jsonOutput = true;
                    break;
                case "--path":
                    if (i + 1 < args.Length)
                    {
                        serverPath = args[i + 1];
                        i++;
                    }

                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "-i":
                case "--insecure":
                    // Certificate validation is always enabled for security
                    break;
            }

            i++;
        }

        // If an API key file path was provided, attempt to read it
        if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyFile))
        {
            try
            {
                if (File.Exists(apiKeyFile))
                {
                    apiKey = (await File.ReadAllTextAsync(apiKeyFile)).Trim();
                }
                else
                {
                    Console.WriteLine($"API key file '{apiKeyFile}' not found.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read API key file: {ex.Message}");
                return 1;
            }
        }

        // If still no API key, try environment variable fallback
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("SPAMMA_API_KEY") ?? string.Empty;
        }

        // If still none and an env file path exists, try reading file path from environment
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var envKeyFile = Environment.GetEnvironmentVariable("SPAMMA_API_KEY_FILE") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(envKeyFile))
            {
                try
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Attempting to read API key file: {envKeyFile}");
                    }

                    if (File.Exists(envKeyFile))
                    {
                        apiKey = (await File.ReadAllTextAsync(envKeyFile)).Trim();
                    }
                }
                catch (Exception)
                {
                    // silently ignore; we'll error out later if still none
                }
            }
        }

    // Append path to server if provided
        if (!string.IsNullOrWhiteSpace(serverPath))
        {
            // Ensure we have leading slash
            if (!serverPath.StartsWith('/'))
            {
                serverPath = $"/{serverPath}";
            }

            // Trim trailing slash on server URL
            serverUrl = serverUrl.TrimEnd('/') + serverPath;
        }

        Console.WriteLine($"Connecting to {serverUrl}...");

        // Allow using HTTP endpoints (insecure) for local development by passing http:// URL
        // Allow http2 unencrypted on http:// targets for local testing
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var httpHandler = new HttpClientHandler();
        if (serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            // For http, no special handling needed
        }

        using var httpClient = new HttpClient(httpHandler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        using var channel = GrpcChannel.ForAddress(serverUrl, new GrpcChannelOptions { HttpClient = httpClient });
        var client = new EmailPushService.EmailPushServiceClient(channel);

        // Prefer API key header - do not use JWT tokens in the payload for this sample.
        var request = new SubscribeRequest();

        Grpc.Core.Metadata? headers = null;
        try
        {
            // Attach API key as authentication metadata header for the call
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                headers = new Grpc.Core.Metadata { { "X-API-Key", apiKey } };
            }
            else
            {
                Console.WriteLine("No API key provided. Please supply --api-key <key> to authenticate.");
                return 1;
            }

            if (verbose)
            {
                Console.WriteLine($"Attempting gRPC call to: {serverUrl}{serviceMethodPath}");
            }

            using var call = client.SubscribeToEmails(request, headers);
            var responseStream = call.ResponseStream;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent immediate termination
                cts.Cancel();
            };

            Console.WriteLine("Subscribed. Waiting for email notifications (Ctrl+C to exit)...");

            while (await responseStream.MoveNext(cts.Token))
            {
                var notif = responseStream.Current;
                var receivedAt = notif.ReceivedAt != null
                    ? DateTimeOffset.FromUnixTimeSeconds(notif.ReceivedAt.Seconds).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : "Unknown";

                if (jsonOutput)
                {
                    var jsonObj = new
                    {
                        Id = notif.Id,
                        To = notif.To,
                        From = notif.From,
                        Subject = notif.Subject,
                        ReceivedAt = receivedAt,
                    };
                    Console.WriteLine(JsonSerializer.Serialize(jsonObj));
                }
                else
                {
                    Console.WriteLine($"[Email] Id={notif.Id}, To={notif.To}, From={notif.From}, Subject=\"{notif.Subject}\", ReceivedAt={receivedAt}");
                }

                // Fetch full email content using GetEmailContent
                try
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Fetching email content for ID: {notif.Id}");
                    }

                    var contentRequest = new GetEmailContentRequest { EmailId = notif.Id };
                    var contentResponse = await client.GetEmailContentAsync(contentRequest, headers, cancellationToken: cts.Token);

                    // Parse MIME message and extract HTML body
                    var mimeMessage = await MimeMessage.LoadAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentResponse.MimeMessage)));

                    // Get HTML body if available, otherwise fall back to text body
                    var body = mimeMessage.HtmlBody ?? mimeMessage.TextBody ?? "(No body content)";

                    if (!jsonOutput)
                    {
                        Console.WriteLine("--- Email Body (HTML) ---");
                        Console.WriteLine(body);
                        Console.WriteLine("--- End of Body ---");
                    }
                    else
                    {
                        // In JSON mode, add the body to the output
                        var jsonObjWithBody = new
                        {
                            Id = notif.Id,
                            To = notif.To,
                            From = notif.From,
                            Subject = notif.Subject,
                            ReceivedAt = receivedAt,
                            HtmlBody = mimeMessage.HtmlBody,
                            TextBody = mimeMessage.TextBody,
                        };
                        Console.WriteLine(JsonSerializer.Serialize(jsonObjWithBody));
                    }

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to fetch email content for ID {notif.Id}: {ex.Message}");
                    if (verbose)
                    {
                        Console.WriteLine($"  Details: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If verbose, give more info
            if (verbose)
            {
                Console.WriteLine($"Error subscribing to email stream (detailed): {ex}");
            }

            Console.WriteLine($"Error subscribing to email stream: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static string GetDefaultServerUrl()
    {
        var host = "localhost";
        var port = 7181;
        return $"https://{host}:{port}";
    }
}