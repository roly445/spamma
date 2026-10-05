using System.Net;
using System.Text.RegularExpressions;
using Marten;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Browser.Tests;

internal sealed class DomainScenarioFixture : IAsyncDisposable
{
    private readonly IDocumentStore store;
    private readonly SmtpCapture smtpCapture;
    private readonly DnsTxtCapture dnsCapture;

    private DomainScenarioFixture(IDocumentStore store, SmtpCapture smtpCapture, DnsTxtCapture dnsCapture, string emailAddress)
    {
        this.store = store;
        this.smtpCapture = smtpCapture;
        this.dnsCapture = dnsCapture;
        this.EmailAddress = emailAddress;
    }

    public string EmailAddress { get; }

    public static async Task<DomainScenarioFixture> CreateAsync(bool administrator = true)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Domain browser tests require a disposable PostgreSQL database.");
        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            Spamma.Modules.UserManagement.Module.ConfigureUserManagement(options);
            Spamma.Modules.DomainManagement.Module.ConfigureDomainManagement(options);
        });

        var id = Guid.NewGuid();
        var emailAddress = $"domain-user-{id:N}@example.test";
        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(id,
                new UserCreated(id, "Domain fixture user", emailAddress, Guid.NewGuid(), DateTime.UnixEpoch,
                    administrator ? SystemRole.DomainManagement | SystemRole.UserManagement : 0));
            session.Store(new UserLookup
            {
                Id = id, Name = "Domain fixture user", EmailAddress = emailAddress,
                CreatedAt = DateTime.UnixEpoch,
                SystemRole = administrator ? SystemRole.DomainManagement | SystemRole.UserManagement : 0,
            });
            await session.SaveChangesAsync();
        }

        var smtpCapture = new SmtpCapture(2527);
        smtpCapture.Start();
        return new DomainScenarioFixture(store, smtpCapture, new DnsTxtCapture(), emailAddress);
    }

    public async Task<(Guid Id, string Name, string Token)> SeedDomainAsync(string name, bool verified = false,
        bool suspended = false, string? contact = null, DateTime? createdAt = null)
    {
        var id = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var events = new List<object> { new DomainCreated(id, name, contact, "Fixture domain", token, createdAt ?? DateTime.UtcNow) };
        if (verified) events.Add(new DomainVerified(DateTime.UtcNow));
        if (suspended) events.Add(new DomainSuspended(Spamma.Modules.DomainManagement.Client.Contracts.DomainSuspensionReason.Administrative,
            "Fixture suspension", DateTime.UtcNow));
        await using var session = this.store.LightweightSession();
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(id, events.ToArray());
        await session.SaveChangesAsync();
        return (id, name, token);
    }

    public async Task<(Guid Id, string Email)> SeedUserAsync(string name)
    {
        var id = Guid.NewGuid();
        var email = $"domain-moderator-{id:N}@example.test";
        await using var session = this.store.LightweightSession();
        session.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(id,
            new UserCreated(id, name, email, Guid.NewGuid(), DateTime.UtcNow, 0));
        session.Store(new UserLookup { Id = id, Name = name, EmailAddress = email, CreatedAt = DateTime.UtcNow, SystemRole = 0 });
        await session.SaveChangesAsync();
        return (id, email);
    }

    public async Task AssignCurrentUserAsync(Guid domainId)
    {
        await using var session = this.store.LightweightSession();
        var user = await session.Query<UserLookup>().FirstAsync(x => x.EmailAddress == this.EmailAddress);
        session.Store(new UserLookup
        {
            Id = user.Id, Name = user.Name, EmailAddress = user.EmailAddress, CreatedAt = user.CreatedAt,
            SystemRole = user.SystemRole, ModeratedDomains = [domainId],
        });
        await session.SaveChangesAsync();
    }

    public async Task<string> WaitForLoginPathAsync()
    {
        var message = await this.smtpCapture.WaitForMessageAsync(TimeSpan.FromSeconds(45));
        var text = message.TextBody ?? message.HtmlBody ?? string.Empty;
        var match = Regex.Match(text, @"https?://[^\s<>""']+/logging-in\?token=[^\s<>""']+", RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("The captured authentication email has no login URL.");
        return new Uri(WebUtility.HtmlDecode(match.Value)).PathAndQuery;
    }

    public void PublishTxt(string name, string token) => this.dnsCapture.Publish(name, token);

    public async ValueTask DisposeAsync()
    {
        await this.smtpCapture.DisposeAsync();
        await this.dnsCapture.DisposeAsync();
        this.store.Dispose();
    }
}
