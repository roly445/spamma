using MailKit.Net.Smtp;
using MailKit.Security;
using Marten;
using MimeKit;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Browser.Tests;

internal sealed class SmtpScenarioFixture : IAsyncDisposable
{
    private readonly IDocumentStore store;
    private readonly bool originalCatchAllMode;

    private SmtpScenarioFixture(DomainScenarioFixture domainFixture, IDocumentStore store,
        bool originalCatchAllMode, Guid subdomainId, string recipient, string? chaosRecipient)
    {
        this.DomainFixture = domainFixture;
        this.store = store;
        this.originalCatchAllMode = originalCatchAllMode;
        this.SubdomainId = subdomainId;
        this.Recipient = recipient;
        this.ChaosRecipient = chaosRecipient;
    }

    public DomainScenarioFixture DomainFixture { get; }
    public string Recipient { get; }
    public Guid SubdomainId { get; }
    public string? ChaosRecipient { get; }

    public static async Task<SmtpScenarioFixture> CreateAsync(bool suspendDomain = false,
        bool suspendSubdomain = false, bool chaos = false)
    {
        var domainFixture = await DomainScenarioFixture.CreateAsync();
        var name = $"smtp-{Guid.NewGuid():N}.example.test";
        var domain = await domainFixture.SeedDomainAsync(name, verified: true, suspended: suspendDomain);
        var subdomainId = await domainFixture.SeedSubdomainAsync(domain.Id, "inbound", suspended: suspendSubdomain);
        await domainFixture.AssignCurrentUserToViewSubdomainAsync(subdomainId);
        if (chaos) await domainFixture.SeedChaosAddressAsync(domain.Id, subdomainId, "chaos", enabled: true);

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("SMTP browser tests require a disposable PostgreSQL database.");
        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(options);
        });
        bool originalCatchAllMode;
        await using (var session = store.QuerySession())
        {
            originalCatchAllMode = (await session.LoadAsync<EmailInboxSettingsDocument>(
                EmailInboxSettingsDocument.SettingsId))?.CatchAllModeEnabled ?? false;
        }
        await using (var session = store.LightweightSession())
        {
            session.Store(new EmailInboxSettingsDocument { CatchAllModeEnabled = false });
            await session.SaveChangesAsync();
        }

        var fullName = $"inbound.{name}";
        return new SmtpScenarioFixture(domainFixture, store, originalCatchAllMode, subdomainId,
            $"receiver@{fullName}", chaos ? $"chaos@{fullName}" : null);
    }

    public async Task<(bool Accepted, int Code, string Response)> SendAsync(string recipient,
        string subject, string? campaignValue = null, string sender = "sender@external.example")
    {
        var message = new MimeMessage
        {
            From = { MailboxAddress.Parse(sender) },
            To = { MailboxAddress.Parse(recipient) },
            Subject = subject,
            Body = new TextPart("plain") { Text = $"SMTP body for {subject}" },
        };
        if (campaignValue is not null) message.Headers.Add("X-Spamma-Camp", campaignValue);

        using var client = new SmtpClient();
        var port = int.TryParse(Environment.GetEnvironmentVariable("SmtpServer__Port"), out var configuredPort)
            ? configuredPort : 2526;
        await client.ConnectAsync("127.0.0.1", port, SecureSocketOptions.None);
        try
        {
            var response = await client.SendAsync(message);
            return (true, 250, response);
        }
        catch (SmtpCommandException exception)
        {
            return (false, (int)exception.StatusCode, exception.Message);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    public async Task<int> CountMessagesAsync(string subject)
    {
        await using var session = this.store.QuerySession();
        return await session.Query<EmailLookup>().CountAsync(x => x.Subject == subject);
    }

    public async Task<EmailLookup> WaitForMessageAsync(string subject)
    {
        var timeout = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < timeout)
        {
            await using var session = this.store.QuerySession();
            var message = await session.Query<EmailLookup>().FirstOrDefaultAsync(x => x.Subject == subject);
            if (message is not null) return message;
            await Task.Delay(250);
        }
        throw new TimeoutException($"SMTP message '{subject}' was not persisted within 40 seconds.");
    }

    public async Task<CampaignSummary> WaitForCampaignAsync(string value)
    {
        var timeout = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < timeout)
        {
            await using var session = this.store.QuerySession();
            var campaign = await session.Query<CampaignSummary>().FirstOrDefaultAsync(x => x.CampaignValue == value);
            if (campaign is not null) return campaign;
            await Task.Delay(250);
        }
        throw new TimeoutException($"Campaign '{value}' was not captured within 40 seconds.");
    }

    public async ValueTask DisposeAsync()
    {
        await using (var session = this.store.LightweightSession())
        {
            session.Store(new EmailInboxSettingsDocument { CatchAllModeEnabled = this.originalCatchAllMode });
            await session.SaveChangesAsync();
        }
        this.store.Dispose();
        await this.DomainFixture.DisposeAsync();
    }
}
