using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Marten;
using Marten.Linq;
using MimeKit;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.Events;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Domain.UserAggregate.Events;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Browser.Tests;

internal sealed class InboxScenarioFixture : IAsyncDisposable
{
    public sealed record SecondUserBoundary(string EmailAddress, string FirstDomainName,
        string SecondDomainName, Guid FirstSubdomainId, Guid SecondSubdomainId, Guid SecondDomainId);

    private readonly IDocumentStore store;
    private readonly SmtpCapture smtpCapture;
    private readonly string messageDirectory;
    private readonly List<Guid> messageIds = [];
    private bool originalCatchAllMode;

    private InboxScenarioFixture(IDocumentStore store, SmtpCapture smtpCapture, string messageDirectory,
        IReadOnlyList<Guid> subdomainIds, Guid domainId, string emailAddress)
    {
        this.store = store;
        this.smtpCapture = smtpCapture;
        this.messageDirectory = messageDirectory;
        this.SubdomainIds = subdomainIds;
        this.DomainId = domainId;
        this.EmailAddress = emailAddress;
    }

    public Guid SubdomainId => this.SubdomainIds[0];

    public IReadOnlyList<Guid> SubdomainIds { get; }

    public Guid DomainId { get; }

    public string EmailAddress { get; }

    public Guid UserId { get; private set; }

    public static async Task<InboxScenarioFixture> CreateAsync(bool restrictedCampaignUser = false, bool viewer = false,
        SystemRole isolatedRole = 0)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Inbox browser tests require a disposable PostgreSQL database.");
        var contentRoot = Environment.GetEnvironmentVariable("SPAMMA_E2E_APP_CONTENT_ROOT")
            ?? throw new InvalidOperationException("SPAMMA_E2E_APP_CONTENT_ROOT must point to the running app's content root.");

        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            Spamma.Modules.UserManagement.Module.ConfigureUserManagement(options);
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(options);
            Spamma.Modules.DomainManagement.Module.ConfigureDomainManagement(options);
        });

        var domainId = Guid.NewGuid();
        var subdomainIds = restrictedCampaignUser ? new[] { Guid.NewGuid(), Guid.NewGuid() } : [Guid.NewGuid()];
        string emailAddress;
        if (restrictedCampaignUser)
        {
            var userId = Guid.NewGuid();
            emailAddress = $"campaign-{userId:N}@example.test";
            await using (var session = store.LightweightSession())
            {
                session.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(userId,
                    new UserCreated(userId, "Campaign fixture user", emailAddress, Guid.NewGuid(), DateTime.UnixEpoch, isolatedRole));
                await session.SaveChangesAsync();
            }

            await using (var session = store.LightweightSession())
            {
                session.Store(new UserLookup
                {
                    Id = userId,
                    Name = "Campaign fixture user",
                    EmailAddress = emailAddress,
                    CreatedAt = DateTime.UnixEpoch,
                    SystemRole = isolatedRole,
                    ModeratedSubdomains = viewer ? [] : subdomainIds,
                    ViewableSubdomains = viewer ? subdomainIds : [],
                });
                for (var index = 0; index < subdomainIds.Length; index++)
                {
                    var name = $"campaign-{(char)('a' + index)}-{subdomainIds[index].ToString("N")[..8]}";
                    session.Store(new SubdomainLookup
                    {
                        Id = subdomainIds[index],
                        DomainId = domainId,
                        SubdomainName = name,
                        FullName = $"{name}.example.test",
                        ParentName = "example.test",
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                await session.SaveChangesAsync();
            }
        }
        else
        {
            await using var session = store.LightweightSession();
            var user = await session.Query<UserLookup>()
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Run the setup administrator scenario before inbox browser tests.");

            emailAddress = user.EmailAddress;
            session.Store(new UserLookup
            {
                Id = user.Id,
                Name = user.Name,
                EmailAddress = user.EmailAddress,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                LastPasskeyAuthenticationAt = user.LastPasskeyAuthenticationAt,
                IsSuspended = user.IsSuspended,
                SuspendedAt = user.SuspendedAt,
                SystemRole = user.SystemRole,
                ModeratedDomains = user.ModeratedDomains,
                ModeratedSubdomains = user.ModeratedSubdomains,
                ViewableSubdomains = [subdomainIds[0]],
            });
            await session.SaveChangesAsync();
        }

        var smtpCapture = new SmtpCapture(2527);
        smtpCapture.Start();
        var messageDirectory = Path.Combine(contentRoot, "messages");
        Directory.CreateDirectory(messageDirectory);
        var fixture = new InboxScenarioFixture(store, smtpCapture, messageDirectory, subdomainIds, domainId, emailAddress);
        await using (var session = store.QuerySession())
        {
            fixture.originalCatchAllMode = (await session.LoadAsync<EmailInboxSettingsDocument>(EmailInboxSettingsDocument.SettingsId))?.CatchAllModeEnabled ?? false;
            fixture.UserId = await session.Query<UserLookup>().Where(x => x.EmailAddress == emailAddress).Select(x => x.Id).FirstAsync();
        }
        return fixture;
    }

    public async Task SetCatchAllModeAsync(bool enabled)
    {
        await using var session = this.store.LightweightSession();
        session.Store(new EmailInboxSettingsDocument { CatchAllModeEnabled = enabled });
        await session.SaveChangesAsync();
    }

    public async Task<SecondUserBoundary> SeedSecondUserBoundaryAsync()
    {
        var firstSubdomainId = Guid.NewGuid();
        var secondSubdomainId = Guid.NewGuid();
        var secondDomainId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var firstDomainName = $"first-{this.DomainId.ToString("N")[..8]}.example.test";
        var secondDomainName = $"second-{secondDomainId.ToString("N")[..8]}.example.test";
        var secondEmail = $"inbox-second-{secondUserId:N}@example.test";

        await using (var eventSession = this.store.LightweightSession())
        {
            eventSession.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(secondUserId,
                new UserCreated(secondUserId, "Second inbox fixture user", secondEmail,
                    Guid.NewGuid(), DateTime.UtcNow, 0));
            await eventSession.SaveChangesAsync();
        }

        var projected = false;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await using var query = this.store.QuerySession();
            if (await query.LoadAsync<UserLookup>(secondUserId) is not null)
            {
                projected = true;
                break;
            }

            await Task.Delay(100);
        }

        if (!projected) throw new InvalidOperationException("The second inbox user projection did not start.");

        await using var session = this.store.LightweightSession();
        var firstUser = await session.LoadAsync<UserLookup>(this.UserId)
            ?? throw new InvalidOperationException("The first inbox user was not projected.");
        session.Store(new UserLookup
        {
            Id = firstUser.Id,
            Name = firstUser.Name,
            EmailAddress = firstUser.EmailAddress,
            CreatedAt = firstUser.CreatedAt,
            SystemRole = firstUser.SystemRole,
            ModeratedDomains = [this.DomainId],
            ViewableSubdomains = [firstSubdomainId],
        });
        session.Store(new UserLookup
        {
            Id = secondUserId,
            Name = "Second inbox fixture user",
            EmailAddress = secondEmail,
            CreatedAt = DateTime.UtcNow,
            ModeratedDomains = [secondDomainId],
            ViewableSubdomains = [secondSubdomainId],
        });
        session.Store(new DomainLookup
        {
            Id = this.DomainId, DomainName = firstDomainName,
            VerificationToken = Guid.NewGuid().ToString("N"),
            IsVerified = true, CreatedAt = DateTime.UtcNow,
        });
        session.Store(new DomainLookup
        {
            Id = secondDomainId, DomainName = secondDomainName,
            VerificationToken = Guid.NewGuid().ToString("N"),
            IsVerified = true, CreatedAt = DateTime.UtcNow,
        });
        session.Store(new SubdomainLookup
        {
            Id = firstSubdomainId, DomainId = this.DomainId,
            SubdomainName = "inbox-first", FullName = $"inbox-first.{firstDomainName}",
            ParentName = firstDomainName, CreatedAt = DateTime.UtcNow,
        });
        session.Store(new SubdomainLookup
        {
            Id = secondSubdomainId, DomainId = secondDomainId,
            SubdomainName = "inbox-second", FullName = $"inbox-second.{secondDomainName}",
            ParentName = secondDomainName, CreatedAt = DateTime.UtcNow,
        });
        await session.SaveChangesAsync();

        return new SecondUserBoundary(secondEmail, firstDomainName, secondDomainName,
            firstSubdomainId, secondSubdomainId, secondDomainId);
    }

    public async Task<(Guid Id, string Email)> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        var email = $"catch-all-user-{id:N}@example.test";
        await using var session = this.store.LightweightSession();
        session.Events.StartStream<Spamma.Modules.UserManagement.Domain.UserAggregate.User>(id,
            new UserCreated(id, "Catch-all assigned user", email, Guid.NewGuid(), DateTime.UtcNow, 0));
        session.Store(new UserLookup { Id = id, Name = "Catch-all assigned user", EmailAddress = email,
            CreatedAt = DateTime.UtcNow, SystemRole = 0 });
        await session.SaveChangesAsync();
        return (id, email);
    }

    public async Task<Guid> SeedCatchAllSenderAsync(string address, bool assignedToCurrentUser = false)
    {
        var id = Guid.NewGuid();
        var events = new List<object> { new CatchAllSenderAddressAdded(id, address, DateTimeOffset.UtcNow) };
        if (assignedToCurrentUser) events.Add(new UserAssignedToCatchAllSender(this.UserId));
        await using var session = this.store.LightweightSession();
        session.Events.StartStream<Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate.CatchAllSenderAddress>(id, events.ToArray());
        session.Store(new CatchAllSenderAddressLookup { Id = id, SenderAddress = address, AddedAt = DateTimeOffset.UtcNow,
            AssignedUserIds = assignedToCurrentUser ? [this.UserId] : [] });
        await session.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> SeedMessageAsync(string subject, string sender = "sender@example.test",
        Guid? subdomainId = null, Guid? campaignId = null, bool includeAttachment = false,
        DateTimeOffset? receivedAt = null, string? campaignValue = null, Guid? catchAllSenderAddressId = null,
        Guid? domainIdOverride = null)
    {
        var id = Guid.NewGuid();
        var actualSubdomainId = catchAllSenderAddressId.HasValue
            ? EmailInboxSettingsDocument.CatchAllSubdomainId : subdomainId ?? this.SubdomainId;
        var received = receivedAt ?? DateTimeOffset.UtcNow;
        var message = new MimeMessage
        {
            Subject = subject,
            Date = received,
        };
        message.From.Add(new MailboxAddress("Fixture Sender", sender));
        message.To.Add(new MailboxAddress("Fixture Recipient", "recipient@example.test"));
        var body = new BodyBuilder
        {
            TextBody = $"Plain body for {subject}",
            HtmlBody = $"<html><body><p>HTML body for {WebUtility.HtmlEncode(subject)}</p></body></html>",
        };
        if (includeAttachment)
        {
            body.Attachments.Add("evidence.txt", Encoding.UTF8.GetBytes("fixture attachment contents"));
        }

        message.Body = body.ToMessageBody();
        await message.WriteToAsync(Path.Combine(this.messageDirectory, $"{id}.eml"));

        await using var session = this.store.LightweightSession();
        var domainId = catchAllSenderAddressId.HasValue
            ? EmailInboxSettingsDocument.CatchAllDomainId : domainIdOverride ?? this.DomainId;
        var receivedEvent = new EmailReceived(id, domainId, actualSubdomainId, subject, received,
        [
            new EmailReceived.EmailAddress(sender, "Fixture Sender", EmailAddressType.From),
            new EmailReceived.EmailAddress("recipient@example.test", "Fixture Recipient", EmailAddressType.To),
        ], catchAllSenderAddressId);
        if (campaignId.HasValue)
        {
            session.Events.StartStream<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(
                id, receivedEvent, new CampaignCaptured(campaignId.Value));
        }
        else
        {
            session.Events.StartStream<Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email>(id, receivedEvent);
        }
        if (campaignId.HasValue)
        {
            session.Store(new CampaignSummary
            {
                CampaignId = campaignId.Value,
                DomainId = domainId,
                SubdomainId = actualSubdomainId,
                CampaignValue = campaignValue ?? "Fixture campaign",
                SampleMessageId = id,
                FirstReceivedAt = received,
                LastReceivedAt = received,
                TotalCaptured = 1,
            });
        }

        await session.SaveChangesAsync();
        this.messageIds.Add(id);
        return id;
    }

    public async Task<Guid> SeedCampaignAsync(string campaignValue, Guid? subdomainId = null,
        DateTimeOffset? receivedAt = null)
    {
        var campaignId = Guid.NewGuid();
        var actualSubdomainId = subdomainId ?? this.SubdomainId;
        var received = receivedAt ?? DateTimeOffset.UtcNow;
        var sampleId = await this.SeedMessageAsync($"Sample for {campaignValue}", subdomainId: actualSubdomainId,
            campaignId: campaignId, receivedAt: received, campaignValue: campaignValue);

        await using var session = this.store.LightweightSession();
        session.Events.StartStream<Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Campaign>(campaignId,
            new Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events.CampaignCreated(
                campaignId, this.DomainId, actualSubdomainId, campaignValue, sampleId,
                received.UtcDateTime, received));
        await session.SaveChangesAsync();
        return campaignId;
    }

    public async Task<Guid> SeedOtherUsersCampaignAsync(string campaignValue)
    {
        var otherSubdomainId = Guid.NewGuid();
        await using (var session = this.store.LightweightSession())
        {
            session.Store(new UserLookup
            {
                Id = Guid.NewGuid(),
                Name = "Other campaign user",
                EmailAddress = $"other-campaign-{Guid.NewGuid():N}@example.test",
                CreatedAt = DateTime.UnixEpoch,
                ViewableSubdomains = [otherSubdomainId],
            });
            await session.SaveChangesAsync();
        }

        return await this.SeedCampaignAsync(campaignValue, otherSubdomainId);
    }

    public async Task<Guid> SeedCampaignSummaryAsync(string campaignValue, Guid subdomainId,
        DateTimeOffset receivedAt, int totalCaptured)
    {
        var campaignId = Guid.NewGuid();
        await using var session = this.store.LightweightSession();
        session.Store(new CampaignSummary
        {
            CampaignId = campaignId,
            DomainId = this.DomainId,
            SubdomainId = subdomainId,
            CampaignValue = campaignValue,
            FirstReceivedAt = receivedAt.AddHours(-1),
            LastReceivedAt = receivedAt,
            TotalCaptured = totalCaptured,
        });
        await session.SaveChangesAsync();
        return campaignId;
    }

    public async Task SeedCampaignSummariesAsync(int count, Guid subdomainId)
    {
        await using var session = this.store.LightweightSession();
        for (var index = 1; index <= count; index++)
        {
            var received = DateTimeOffset.UtcNow.AddMinutes(-index);
            session.Store(new CampaignSummary
            {
                CampaignId = Guid.NewGuid(),
                DomainId = this.DomainId,
                SubdomainId = subdomainId,
                CampaignValue = $"Campaign {index:D2}",
                FirstReceivedAt = received.AddHours(-1),
                LastReceivedAt = received,
                TotalCaptured = index,
            });
        }

        await session.SaveChangesAsync();
    }

    public async Task<Guid> SeedOtherUsersMessageAsync(string subject)
    {
        var otherSubdomainId = Guid.NewGuid();
        await using (var session = this.store.LightweightSession())
        {
            session.Store(new UserLookup
            {
                Id = Guid.NewGuid(),
                Name = "Other fixture user",
                EmailAddress = $"other-{Guid.NewGuid():N}@example.test",
                CreatedAt = DateTime.UnixEpoch,
                ViewableSubdomains = [otherSubdomainId],
            });
            await session.SaveChangesAsync();
        }

        return await this.SeedMessageAsync(subject, subdomainId: otherSubdomainId);
    }

    public async Task<string> WaitForLoginPathAsync()
    {
        var message = await this.smtpCapture.WaitForMessageAsync(TimeSpan.FromSeconds(45));
        var text = message.TextBody ?? message.HtmlBody ?? string.Empty;
        var match = Regex.Match(text, @"https?://[^\s<>""']+/logging-in\?token=[^\s<>""']+", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException("The captured authentication email has no login URL.");
        }

        var uri = new Uri(WebUtility.HtmlDecode(match.Value));
        return uri.PathAndQuery;
    }

    public async ValueTask DisposeAsync()
    {
        await this.smtpCapture.DisposeAsync();
        await this.SetCatchAllModeAsync(this.originalCatchAllMode);
        foreach (var id in this.messageIds)
        {
            var path = Path.Combine(this.messageDirectory, $"{id}.eml");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        this.store.Dispose();
    }
}

internal sealed class SmtpCapture(int port) : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, port);
    private readonly Channel<MimeMessage> messages = Channel.CreateUnbounded<MimeMessage>();
    private readonly CancellationTokenSource cancellation = new();
    private Task? acceptTask;
    private int messageCount;

    public int MessageCount => Volatile.Read(ref this.messageCount);

    public void Start()
    {
        this.listener.Start();
        this.acceptTask = this.AcceptAsync();
    }

    public async Task<MimeMessage> WaitForMessageAsync(TimeSpan timeout) =>
        await this.messages.Reader.ReadAsync().AsTask().WaitAsync(timeout);

    public async ValueTask DisposeAsync()
    {
        await this.cancellation.CancelAsync();
        this.listener.Stop();
        if (this.acceptTask is not null)
        {
            try
            {
                await this.acceptTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
        }

        this.cancellation.Dispose();
    }

    private async Task AcceptAsync()
    {
        while (!this.cancellation.IsCancellationRequested)
        {
            using var client = await this.listener.AcceptTcpClientAsync(this.cancellation.Token);
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
            {
                NewLine = "\r\n",
                AutoFlush = true,
            };
            await writer.WriteLineAsync("220 localhost ESMTP");
            var data = new StringBuilder();
            var inData = false;
            while (await reader.ReadLineAsync(this.cancellation.Token) is { } line)
            {
                if (inData)
                {
                    if (line == ".")
                    {
                        using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(data.ToString()));
                        var capturedMessage = await MimeMessage.LoadAsync(messageStream);
                        Interlocked.Increment(ref this.messageCount);
                        this.messages.Writer.TryWrite(capturedMessage);
                        await writer.WriteLineAsync("250 Queued");
                        inData = false;
                    }
                    else
                    {
                        data.AppendLine(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line);
                    }

                    continue;
                }

                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 localhost");
                }
                else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 End with <CRLF>.<CRLF>");
                    inData = true;
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Goodbye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }
    }
}
