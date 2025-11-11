using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;

namespace GrpcEmailPushClient;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // CLI parsing: --server <url> --jwt <token> --insecure
    var serverUrl = "https://localhost:7181";
    var jwt = string.Empty;
    var apiKey = string.Empty;
        var acceptAnyCert = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    Console.WriteLine("Usage: GrpcEmailPushClient [--server <url>] [--jwt <token>] [--insecure]");
                    return 0;
                case "-s":
                case "--server":
                    if (i + 1 < args.Length)
                    {
                        serverUrl = args[++i];
                    }
                    break;
                case "-j":
                case "--jwt":
                    if (i + 1 < args.Length)
                    {
                        jwt = args[++i];
                    }
                    break;
                case "-k":
                case "--api-key":
                case "--key":
                    if (i + 1 < args.Length)
                    {
                        apiKey = args[++i];
                    }
                    break;
                case "-i":
                case "--insecure":
                    acceptAnyCert = true;
                    break;
            }
        }

        Console.WriteLine($"Connecting to {serverUrl}...");

        // Allow using HTTP endpoints (insecure) for local development by passing http:// URL
        var httpHandler = new HttpClientHandler();
    if (serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            // For http, no special handling needed, but disable TLS validation when using https with self-signed certs
        }
        else
        {
            // If you want to accept self-signed certificates in dev, uncomment the following line.
            if (acceptAnyCert)
            {
                httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
        }

        using var httpClient = new HttpClient(httpHandler);
        using var channel = GrpcChannel.ForAddress(serverUrl, new GrpcChannelOptions { HttpClient = httpClient });
        var client = new EmailPushService.EmailPushServiceClient(channel);

    // Prefer API key header; jwt will be included in the request payload only when provided
    var request = new SubscribeRequest { JwtToken = jwt };

        try
        {
            // Optionally attach JWT as metadata header for the call (Authorization Bearer)
            Grpc.Core.Metadata? headers = null;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // The server expects the API key in the X-API-Key header by default
                headers = new Grpc.Core.Metadata { { "X-API-Key", apiKey } };
            }
            else if (!string.IsNullOrWhiteSpace(jwt))
            {
                // Fallback to JWT Authorization header if provided
                headers = new Grpc.Core.Metadata { { "Authorization", $"Bearer {jwt}" } };
            }

            using var call = client.SubscribeToEmails(request, headers);
            var responseStream = call.ResponseStream;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
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
                Console.WriteLine($"[Email] Id={notif.EmailId}, To={notif.To}, From={notif.From}, Subject=\"{notif.Subject}\", ReceivedAt={receivedAt}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to email stream: {ex.Message}");
            return 1;
        }

        return 0;
    }
}
