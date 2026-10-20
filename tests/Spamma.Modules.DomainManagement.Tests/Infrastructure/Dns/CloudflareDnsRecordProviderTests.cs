using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.Dns;

public class CloudflareDnsRecordProviderTests
{
    private static readonly DnsConnection Connection = new()
    {
        Provider = "Cloudflare",
        ZoneName = "example.com",
        ZoneId = "zone-id",
        Credential = "test-token",
    };

    [Fact]
    public async Task EnsureAsync_ExistingVerificationRecord_DoesNotCreateDuplicate()
    {
        var handler = new FakeHttpHandler(
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[{\"type\":\"TXT\",\"name\":\"example.com\",\"content\":\"spamma-verification=abc\"}],\"result_info\":{\"total_pages\":1}}");
        var provider = CreateProvider(handler);

        var result = await provider.EnsureAsync(Connection, new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.AlreadyPresent, result.Status);
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain(handler.Requests, x => x.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task EnsureAsync_UnrelatedTxtRecord_CreatesVerificationRecord()
    {
        var handler = new FakeHttpHandler(
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[{\"type\":\"TXT\",\"name\":\"example.com\",\"content\":\"v=spf1 -all\"}],\"result_info\":{\"total_pages\":1}}",
            "{\"success\":true,\"result\":{\"id\":\"record-id\"}}");
        var provider = CreateProvider(handler);

        var result = await provider.EnsureAsync(Connection, new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Created, result.Status);
        Assert.Single(handler.Requests, x => x.Method == HttpMethod.Post);
        Assert.Contains("spamma-verification=abc", handler.Requests[^1].Body, StringComparison.Ordinal);
        Assert.DoesNotContain("priority", handler.Requests[^1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureAsync_ConflictingMxRecord_DoesNotOverwrite()
    {
        var handler = new FakeHttpHandler(
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[{\"type\":\"MX\",\"name\":\"inbox.example.com\",\"content\":\"other.example.com\",\"priority\":10}],\"result_info\":{\"total_pages\":1}}");
        var provider = CreateProvider(handler);

        var result = await provider.EnsureAsync(Connection, new(DnsRecordType.Mx, "inbox.example.com", "mail.example.com", 3600, 10), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Conflict, result.Status);
        Assert.DoesNotContain(handler.Requests, x => x.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task EnsureAsync_MissingMxRecord_CreatesWithPriority()
    {
        var handler = new FakeHttpHandler(
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[],\"result_info\":{\"total_pages\":1}}",
            "{\"success\":true,\"result\":{\"id\":\"record-id\"}}");
        var provider = CreateProvider(handler);

        var result = await provider.EnsureAsync(Connection, new(DnsRecordType.Mx, "inbox.example.com", "mail.example.com", 3600, 10), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Created, result.Status);
        Assert.Contains("\"type\":\"MX\"", handler.Requests[^1].Body, StringComparison.Ordinal);
        Assert.Contains("\"priority\":10", handler.Requests[^1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureAsync_ZoneMismatch_StopsBeforeReadingRecords()
    {
        var handler = new FakeHttpHandler("{\"success\":true,\"result\":{\"name\":\"wrong.example\"}}");
        var provider = CreateProvider(handler);

        var result = await provider.EnsureAsync(Connection, new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Failed, result.Status);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task EnsureAsync_ExistingRecordOnSecondPage_DoesNotCreateDuplicate()
    {
        var handler = new FakeHttpHandler(
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[],\"result_info\":{\"total_pages\":2}}",
            "{\"success\":true,\"result\":[{\"type\":\"TXT\",\"name\":\"example.com\",\"content\":\"spamma-verification=abc\"}],\"result_info\":{\"total_pages\":2}}");

        var result = await CreateProvider(handler).EnsureAsync(
            Connection,
            new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.AlreadyPresent, result.Status);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("page=2", handler.Requests[^1].Uri);
        Assert.DoesNotContain(handler.Requests, x => x.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task EnsureAsync_ZoneReadRateLimited_RetriesBeforeCreating()
    {
        var handler = new FakeHttpHandler(
            "{}",
            "{\"success\":true,\"result\":{\"name\":\"example.com\"}}",
            "{\"success\":true,\"result\":[],\"result_info\":{\"total_pages\":1}}",
            "{\"success\":true,\"result\":{\"id\":\"record-id\"}}");
        handler.Statuses.Add(HttpStatusCode.TooManyRequests);

        var result = await CreateProvider(handler).EnsureAsync(
            Connection,
            new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Created, result.Status);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Single(handler.Requests, x => x.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task EnsureAsync_ForbiddenToken_ReportsPermissionFailure()
    {
        var handler = new FakeHttpHandler("{}");
        handler.Statuses.Add(HttpStatusCode.Forbidden);

        var result = await CreateProvider(handler).EnsureAsync(
            Connection,
            new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Failed, result.Status);
        Assert.Contains("permissions", result.Message);
        Assert.Single(handler.Requests);
    }

    private static CloudflareDnsRecordProvider CreateProvider(FakeHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.cloudflare.test/client/v4/") }, NullLogger<CloudflareDnsRecordProvider>.Instance);

    private sealed class FakeHttpHandler(params string[] responses) : HttpMessageHandler
    {
        private int nextResponse;

        public List<(HttpMethod Method, string Body, string Uri)> Requests { get; } = [];

        public List<HttpStatusCode> Statuses { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            this.Requests.Add((request.Method, body, request.RequestUri?.ToString() ?? string.Empty));
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            var index = this.nextResponse++;
            return new HttpResponseMessage(index < this.Statuses.Count ? this.Statuses[index] : HttpStatusCode.OK)
            {
                Content = new StringContent(responses[index], Encoding.UTF8, "application/json"),
            };
        }
    }
}
