using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed class WebhookFeedbackSender(HttpClient httpClient, IDataProtectionProvider protectionProvider)
{
    public static SocketsHttpHandler CreateSafeHandler() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(address => !WebhookDestinationPolicy.IsPublicAddress(address)))
            {
                throw new InvalidOperationException("Webhook DNS resolved to a non-public address.");
            }

            var socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };

    public async Task SendAsync(SpamReport report, CancellationToken cancellationToken)
    {
        if (report.WebhookUrl is null || report.ProtectedWebhookSecret is null ||
            !WebhookDestinationPolicy.IsAllowedUrl(report.WebhookUrl))
        {
            throw new InvalidOperationException("Webhook destination is not valid.");
        }

        var secret = protectionProvider.CreateProtector("Spamma.SpamFeedback.WebhookSecret.v1")
            .Unprotect(report.ProtectedWebhookSecret);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var payload = JsonSerializer.Serialize(new
        {
            type = "spam.reported",
            reportId = report.Id,
            messageId = report.Id,
            recipient = report.Recipient,
            sender = report.Sender,
            subdomainId = report.SubdomainId,
            campaignId = report.CampaignId,
            campaignValue = report.CampaignValue,
            trigger = report.Trigger.ToString(),
            reportedAt = report.CreatedAt,
        });
        var signatureBytes = HMACSHA256.HashData(
            Convert.FromHexString(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, report.WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Spamma-Report-Id", report.Id.ToString());
        request.Headers.Add("X-Spamma-Timestamp", timestamp);
        request.Headers.Add("X-Spamma-Signature", $"sha256={Convert.ToHexString(signatureBytes).ToLowerInvariant()}");
        request.Headers.Add("Idempotency-Key", report.Id.ToString());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
