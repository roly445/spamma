using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.UserManagement;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Domain.ApiKeys.Events;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.EmailInbox.Tests.E2E;

[Collection("SmtpE2E")]
public sealed class GrpcApiKeyLifecycleTests : IAsyncLifetime
{
    private readonly Guid ownerId = Guid.NewGuid();
    private readonly Guid emailId = Guid.NewGuid();
    private PostgreSqlContainer? postgres;
    private IHost? host;
    private TestServer? server;
    private GrpcChannel? channel;
    private EmailPushService.EmailPushServiceClient? client;

    public async Task InitializeAsync()
    {
        this.postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_grpc_api_key_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
        await this.postgres.StartAsync();

        this.host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddGrpc();
                services.AddDistributedMemoryCache();
                services.AddMarten(options =>
                {
                    options.Connection(this.postgres.GetConnectionString());
                    options.RestoreV8Defaults();
                    options.DatabaseSchemaName = "public";
                    Spamma.Modules.UserManagement.Module.ConfigureUserManagement(options);
                }).UseIdentitySessions();
                services.AddUserManagement();
                services.AddSingleton<PushNotificationManager>();
                services.AddScoped<EmailPushGrpcService>();
                services.AddSingleton<IEmailNotificationAccessService>(
                    new TestEmailAccess(this.ownerId, this.emailId));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGrpcService<EmailPushGrpcService>());
            }))
            .StartAsync();
        this.server = this.host.GetTestServer();

        var store = this.server.Services.GetRequiredService<IDocumentStore>();
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        var httpClient = this.server.CreateClient();
        this.channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient,
        });
        this.client = new EmailPushService.EmailPushServiceClient(this.channel);
    }

    public async Task DisposeAsync()
    {
        this.channel?.Dispose();
        if (this.host is not null)
        {
            await this.host.StopAsync();
            this.host.Dispose();
        }

        if (this.postgres is not null)
        {
            await this.postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetEmailContent_RejectsSameKeyAfterRevocation()
    {
        var key = await this.CreateKeyAsync(DateTime.UtcNow.AddMinutes(10));

        var response = await this.GetEmailContentAsync(key.Value);
        Assert.Contains("Placeholder email content", response.MimeMessage);

        await using (var session = this.server!.Services.GetRequiredService<IDocumentStore>().LightweightSession())
        {
            session.Events.Append(key.Id, new ApiKeyRevoked(DateTime.UtcNow));
            await session.SaveChangesAsync();
        }

        var error = await Assert.ThrowsAsync<RpcException>(() => this.GetEmailContentAsync(key.Value));
        Assert.Equal(StatusCode.Unauthenticated, error.StatusCode);
    }

    [Fact]
    public async Task GetEmailContent_RejectsSameKeyAfterExpiry()
    {
        var expiresAt = DateTime.UtcNow.AddSeconds(10);
        var key = await this.CreateKeyAsync(expiresAt);

        var response = await this.GetEmailContentAsync(key.Value);
        Assert.Contains("Placeholder email content", response.MimeMessage);

        await Task.Delay(expiresAt - DateTime.UtcNow + TimeSpan.FromMilliseconds(100));

        var error = await Assert.ThrowsAsync<RpcException>(() => this.GetEmailContentAsync(key.Value));
        Assert.Equal(StatusCode.Unauthenticated, error.StatusCode);
    }

    private async Task<(Guid Id, string Value)> CreateKeyAsync(DateTime expiresAt)
    {
        var id = Guid.NewGuid();
        var value = $"sk-{Guid.NewGuid():N}";
        var prefix = value[..8];
        var prefixHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(prefix)));

        await using var session = this.server!.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        session.Events.StartStream<ApiKey>(id, new ApiKeyCreated(
            id,
            this.ownerId,
            "gRPC lifecycle test",
            prefixHash,
            BCrypt.Net.BCrypt.HashPassword(value),
            DateTime.UtcNow,
            expiresAt));
        await session.SaveChangesAsync();
        return (id, value);
    }

    private async Task<GetEmailContentResponse> GetEmailContentAsync(string key)
    {
        var headers = new Metadata { { "x-api-key", key } };
        return await this.client!.GetEmailContentAsync(
            new GetEmailContentRequest { EmailId = this.emailId.ToString() },
            headers);
    }

    private sealed class TestEmailAccess(Guid ownerId, Guid expectedEmailId) : IEmailNotificationAccessService
    {
        public Task<bool> CanAccessNotificationAsync(Guid userId, PushNotificationManager.EmailDetails email, CancellationToken cancellationToken) =>
            Task.FromResult(userId == ownerId);

        public Task<bool> CanAccessEmailAsync(Guid userId, Guid emailId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == ownerId && emailId == expectedEmailId);
    }
}
