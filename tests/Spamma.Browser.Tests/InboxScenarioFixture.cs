using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Marten;
using Marten.Linq;
using MimeKit;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Browser.Tests;

internal sealed class InboxScenarioFixture : IAsyncDisposable
{
    private readonly IDocumentStore store;
    private readonly SmtpCapture smtpCapture;
    private readonly string messageDirectory;
    private readonly List<Guid> messageIds = [];

    private InboxScenarioFixture(IDocumentStore store, SmtpCapture smtpCapture, string messageDirectory,
        Guid subdomainId, string emailAddress)
    {
        this.store = store;
        this.smtpCapture = smtpCapture;
        this.messageDirectory = messageDirectory;
        this.SubdomainId = subdomainId;
        this.EmailAddress = emailAddress;
    }

    public Guid SubdomainId { get; }

    public string EmailAddress { get; }

    public static async Task<InboxScenarioFixture> CreateAsync()
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
        });

        var subdomainId = Guid.NewGuid();
        string emailAddress;
        await using (var session = store.LightweightSession())
        {
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
                ViewableSubdomains = [subdomainId],
            });
            await session.SaveChangesAsync();
        }

        var smtpCapture = new SmtpCapture(2527);
        smtpCapture.Start();
        var messageDirectory = Path.Combine(contentRoot, "messages");
        Directory.CreateDirectory(messageDirectory);
        return new InboxScenarioFixture(store, smtpCapture, messageDirectory, subdomainId, emailAddress);
    }

    public async Task<Guid> SeedMessageAsync(string subject, string sender = "sender@example.test",
        Guid? subdomainId = null, Guid? campaignId = null, bool includeAttachment = false,
        DateTimeOffset? receivedAt = null)
    {
        var id = Guid.NewGuid();
        var actualSubdomainId = subdomainId ?? this.SubdomainId;
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
        var domainId = Guid.NewGuid();
        var receivedEvent = new EmailReceived(id, domainId, actualSubdomainId, subject, received,
        [
            new EmailReceived.EmailAddress(sender, "Fixture Sender", EmailAddressType.From),
            new EmailReceived.EmailAddress("recipient@example.test", "Fixture Recipient", EmailAddressType.To),
        ]);
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
                CampaignValue = "Fixture campaign",
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
    private readonly TaskCompletionSource<MimeMessage> message = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource cancellation = new();
    private Task? acceptTask;

    public void Start()
    {
        this.listener.Start();
        this.acceptTask = this.AcceptAsync();
    }

    public async Task<MimeMessage> WaitForMessageAsync(TimeSpan timeout) =>
        await this.message.Task.WaitAsync(timeout);

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
                        this.message.TrySetResult(await MimeMessage.LoadAsync(messageStream));
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
