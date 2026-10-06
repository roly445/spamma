using System.Net;
using System.Text.RegularExpressions;
using Marten;
using Marten.Patching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Browser.Tests;

internal sealed class AuthenticationScenarioFixture : IAsyncDisposable
{
    private readonly IDocumentStore store;
    private readonly SmtpCapture smtpCapture;
    private readonly string connectionString;
    private readonly Guid securityStamp;

    private AuthenticationScenarioFixture(IDocumentStore store, SmtpCapture smtpCapture,
        string connectionString, Guid userId, Guid securityStamp, string emailAddress)
    {
        this.store = store;
        this.smtpCapture = smtpCapture;
        this.connectionString = connectionString;
        this.UserId = userId;
        this.securityStamp = securityStamp;
        this.EmailAddress = emailAddress;
    }

    public Guid UserId { get; }

    public string EmailAddress { get; }

    public int CapturedMessageCount => this.smtpCapture.MessageCount;

    public async Task<int> CountStartedAttemptsAsync()
    {
        await using var session = this.store.QuerySession();
        var events = await session.Events.FetchStreamAsync(this.UserId);
        return events.Count(x => x.Data is AuthenticationStarted);
    }

    public static async Task<AuthenticationScenarioFixture> CreateAsync(bool suspended = false)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Authentication browser tests require a disposable PostgreSQL database.");
        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            Spamma.Modules.UserManagement.Module.ConfigureUserManagement(options);
        });

        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var emailAddress = $"auth-{userId:N}@example.test";
        await using (var session = store.LightweightSession())
        {
            var events = new List<object>
            {
                new UserCreated(userId, "Authentication fixture user", emailAddress,
                    securityStamp, DateTime.UtcNow, 0),
            };
            if (suspended)
            {
                events.Add(new AccountSuspended(AccountSuspensionReason.Administrative,
                    "Fixture suspension", DateTime.UtcNow, Guid.NewGuid()));
            }

            session.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(userId, events.ToArray());
            session.Store(new UserLookup
            {
                Id = userId,
                Name = "Authentication fixture user",
                EmailAddress = emailAddress,
                CreatedAt = DateTime.UtcNow,
                IsSuspended = suspended,
                SuspendedAt = suspended ? DateTime.UtcNow : null,
            });
            await session.SaveChangesAsync();
        }

        var smtpCapture = new SmtpCapture(2527);
        smtpCapture.Start();
        return new AuthenticationScenarioFixture(store, smtpCapture, connectionString,
            userId, securityStamp, emailAddress);
    }

    public async Task<string> WaitForLoginPathAsync()
    {
        var message = await this.smtpCapture.WaitForMessageAsync(TimeSpan.FromSeconds(45));
        var recipients = message.To.Mailboxes.Select(x => x.Address);
        if (!recipients.Contains(this.EmailAddress, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The login message was not sent to the fixture account.");
        }

        var text = message.TextBody ?? message.HtmlBody ?? string.Empty;
        var match = Regex.Match(text, @"https?://[^\s<>""']+/logging-in\?token=[^\s<>""']+", RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("The captured login message has no confirmation URL.");
        return new Uri(WebUtility.HtmlDecode(match.Value)).PathAndQuery;
    }

    public async Task SuspendAsync()
    {
        await using var session = this.store.LightweightSession();
        session.Events.Append(this.UserId, new AccountSuspended(AccountSuspensionReason.Administrative,
            "Fixture suspension", DateTime.UtcNow, Guid.NewGuid()));
        session.Patch<UserLookup>(this.UserId)
            .Set(x => x.IsSuspended, true)
            .Set(x => x.SuspendedAt, DateTime.UtcNow);
        await session.SaveChangesAsync();
    }

    public async Task<string> SeedExpiredLoginPathAsync()
    {
        var attemptId = Guid.NewGuid();
        await using (var session = this.store.LightweightSession())
        {
            session.Events.Append(this.UserId,
                new AuthenticationStarted(attemptId, DateTime.UtcNow.AddMinutes(-16)));
            await session.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(this.connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT value FROM app_configuration WHERE key = 'security.signingKey'", connection);
        var signingKey = (string?)await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The browser app has no configured signing key.");
        var provider = new AuthTokenProvider(
            Options.Create(new Settings { SigningKeyBase64 = signingKey }),
            NullLogger<AuthTokenProvider>.Instance);
        var token = provider.GenerateAuthenticationToken(
            new IAuthTokenProvider.AuthenticationTokenModel(
                this.UserId, this.securityStamp, DateTime.UtcNow, attemptId));
        if (token.IsFailure) throw new InvalidOperationException("Could not generate an aged login-attempt token.");
        return $"/logging-in?token={Uri.EscapeDataString(token.Value)}";
    }

    public async ValueTask DisposeAsync()
    {
        await this.smtpCapture.DisposeAsync();
        this.store.Dispose();
    }
}
