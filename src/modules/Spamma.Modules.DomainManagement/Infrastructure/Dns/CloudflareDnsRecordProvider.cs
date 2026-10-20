using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Infrastructure.Dns;

internal sealed class CloudflareDnsRecordProvider(HttpClient client, ILogger<CloudflareDnsRecordProvider> logger) : IDnsRecordProvider
{
    public string Name => "Cloudflare";

    public async Task<DnsSetupResult> EnsureAsync(DnsConnection connection, DnsRecordSpec record, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connection.ZoneId) || string.IsNullOrWhiteSpace(connection.Credential))
        {
            return new(DnsSetupStatus.NotConfigured, "The Cloudflare zone ID or API token is missing.");
        }

        try
        {
            using var zoneResponse = await this.SendGetWithRetryAsync($"zones/{Uri.EscapeDataString(connection.ZoneId)}", connection.Credential, cancellationToken);
            if (!zoneResponse.IsSuccessStatusCode)
            {
                return ApiFailure(zoneResponse.StatusCode);
            }

            using var zoneJson = await JsonDocument.ParseAsync(await zoneResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!Succeeded(zoneJson.RootElement) ||
                !(zoneJson.RootElement.GetProperty("result").GetProperty("name").GetString() ?? string.Empty).TrimEnd('.').Equals(connection.ZoneName.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
            {
                return new(DnsSetupStatus.Failed, "The Cloudflare zone does not match the configured domain.");
            }

            var records = await this.ListRecordsAsync(connection, record, cancellationToken);
            if (records.Error is not null)
            {
                return records.Error;
            }

            if ((record.Type == DnsRecordType.Mx && records.Records.Count > 1) || records.Records.Any(x => IsConflict(x, record)))
            {
                return new(DnsSetupStatus.Conflict, "A conflicting DNS record exists. Review it in Cloudflare before continuing.");
            }

            if (records.Records.Any(existing => Matches(existing, record)))
            {
                return new(DnsSetupStatus.AlreadyPresent, "The matching DNS record already exists in Cloudflare. Check public DNS to confirm it is live.");
            }

            using var createRequest = NewRequest(HttpMethod.Post, $"zones/{Uri.EscapeDataString(connection.ZoneId)}/dns_records", connection.Credential);
            var payload = new Dictionary<string, object>
            {
                ["type"] = record.Type == DnsRecordType.Txt ? "TXT" : "MX",
                ["name"] = record.Name,
                ["content"] = record.Content,
                ["ttl"] = record.Ttl,
            };
            if (record.Priority.HasValue)
            {
                payload["priority"] = record.Priority.Value;
            }

            createRequest.Content = JsonContent.Create(payload);
            using var createResponse = await client.SendAsync(createRequest, cancellationToken);
            if (!createResponse.IsSuccessStatusCode)
            {
                return ApiFailure(createResponse.StatusCode);
            }

            using var createdJson = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return Succeeded(createdJson.RootElement)
                ? new(DnsSetupStatus.Created, "DNS record submitted to Cloudflare. Check public DNS to confirm it is live.")
                : new(DnsSetupStatus.Failed, "Cloudflare did not accept the DNS record.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(DnsSetupStatus.Failed, "Cloudflare timed out. Retry the action or add the record manually.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Cloudflare DNS request failed for zone {ZoneName}", connection.ZoneName);
            return new(DnsSetupStatus.Failed, "Cloudflare is unavailable. Retry the action or add the record manually.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cloudflare DNS response could not be read for zone {ZoneName}", connection.ZoneName);
            return new(DnsSetupStatus.Failed, "Cloudflare returned an unexpected response.");
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Cloudflare DNS response was missing data for zone {ZoneName}", connection.ZoneName);
            return new(DnsSetupStatus.Failed, "Cloudflare returned an unexpected response.");
        }
    }

    private static HttpRequestMessage NewRequest(HttpMethod method, string path, string credential)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        return request;
    }

    private static bool Succeeded(JsonElement root) => root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;

    private static DnsSetupResult ApiFailure(System.Net.HttpStatusCode status) => status switch
    {
        System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => new(DnsSetupStatus.Failed, "Cloudflare rejected the API token or its zone permissions."),
        System.Net.HttpStatusCode.TooManyRequests => new(DnsSetupStatus.Failed, "Cloudflare rate limited the request. Retry later."),
        _ => new(DnsSetupStatus.Failed, $"Cloudflare DNS request failed (HTTP {(int)status})."),
    };

    private static bool Matches(CloudflareRecord existing, DnsRecordSpec desired) =>
        existing.Content.TrimEnd('.').Equals(
            desired.Content.TrimEnd('.'),
            desired.Type == DnsRecordType.Txt ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) &&
        (desired.Type != DnsRecordType.Mx || existing.Priority == desired.Priority);

    private static bool IsConflict(CloudflareRecord existing, DnsRecordSpec desired) =>
        !Matches(existing, desired) &&
        (desired.Type == DnsRecordType.Mx || existing.Content.StartsWith("spamma-verification=", StringComparison.OrdinalIgnoreCase));

    private async Task<HttpResponseMessage> SendGetWithRetryAsync(string path, string credential, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var request = NewRequest(HttpMethod.Get, path, credential);
            var response = await client.SendAsync(request, cancellationToken);
            if (attempt == 2 || ((int)response.StatusCode != 429 && (int)response.StatusCode < 500))
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), cancellationToken);
        }

        throw new InvalidOperationException("DNS retry loop exited unexpectedly.");
    }

    private async Task<(List<CloudflareRecord> Records, DnsSetupResult? Error)> ListRecordsAsync(
        DnsConnection connection, DnsRecordSpec record, CancellationToken cancellationToken)
    {
        var records = new List<CloudflareRecord>();
        var type = record.Type == DnsRecordType.Txt ? "TXT" : "MX";
        for (var page = 1; page <= 100; page++)
        {
            var path = $"zones/{Uri.EscapeDataString(connection.ZoneId)}/dns_records?type={type}&name.exact={Uri.EscapeDataString(record.Name)}&page={page}&per_page=100";
            using var response = await this.SendGetWithRetryAsync(path, connection.Credential, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (records, ApiFailure(response.StatusCode));
            }

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = json.RootElement;
            if (!Succeeded(root))
            {
                return (records, new(DnsSetupStatus.Failed, "Cloudflare could not list DNS records."));
            }

            foreach (var item in root.GetProperty("result").EnumerateArray())
            {
                if (!(item.GetProperty("type").GetString() ?? string.Empty).Equals(type, StringComparison.OrdinalIgnoreCase) ||
                    !(item.GetProperty("name").GetString() ?? string.Empty).TrimEnd('.').Equals(record.Name.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                records.Add(new CloudflareRecord(
                    item.GetProperty("content").GetString() ?? string.Empty,
                    item.TryGetProperty("priority", out var priority) && priority.ValueKind == JsonValueKind.Number ? priority.GetInt32() : null));
            }

            if (!root.TryGetProperty("result_info", out var info) || !info.TryGetProperty("total_pages", out var pages) || page >= pages.GetInt32())
            {
                return (records, null);
            }
        }

        return (records, new(DnsSetupStatus.Failed, "Cloudflare returned too many DNS record pages to inspect safely."));
    }

    private sealed record CloudflareRecord(string Content, int? Priority);
}
