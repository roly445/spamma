using System;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;

namespace GrpcEmailPushClient;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
    // CLI parsing: --server <url> --api-key <key> --insecure
    var serverUrl = "https://localhost:7181";
    var apiKey = string.Empty;
    var apiKeyFile = string.Empty;
        var acceptAnyCert = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    Console.WriteLine("Usage: GrpcEmailPushClient [--server <url>] --api-key <key> [--insecure]");
                    return 0;
                case "-s":
                case "--server":
                    if (i + 1 < args.Length)
                    {
                        serverUrl = args[++i];
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
                case "-f":
                case "--api-key-file":
                case "--key-file":
                    if (i + 1 < args.Length)
                    {
                        apiKeyFile = args[++i];
                    }
                    break;
                case "-i":
                case "--insecure":
                    acceptAnyCert = true;
                    break;
            }
        }

        // If an API key file path was provided, attempt to read it
        if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyFile))
        {
            try
            {
                if (File.Exists(apiKeyFile))
                {
                    apiKey = File.ReadAllText(apiKeyFile).Trim();
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
                    if (File.Exists(envKeyFile))
                    {
                        apiKey = File.ReadAllText(envKeyFile).Trim();
                    }
                }
                catch (Exception)
                {
                    // silently ignore; we'll error out later if still none
                }
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

    // Prefer API key header - do not use JWT tokens in the payload for this sample.
    var request = new SubscribeRequest();

        try
        {
            // Attach API key as authentication metadata header for the call
            Grpc.Core.Metadata? headers = null;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                headers = new Grpc.Core.Metadata { { "X-API-Key", apiKey } };
            }
            else
            {
                Console.WriteLine("No API key provided. Please supply --api-key <key> to authenticate.");
                return 1;
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
