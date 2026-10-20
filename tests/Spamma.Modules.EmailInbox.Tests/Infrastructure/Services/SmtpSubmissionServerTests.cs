using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Marten;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SmtpSubmissionServerTests
{
    [Fact]
    public async Task TlsSubmissionAuthenticatesAndScopesEnvelopeRecipients()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var otherDomainId = Guid.NewGuid();
        var otherSubdomainId = Guid.NewGuid();
        var policy = new SmtpSubmissionPolicy(
            SmtpSubmissionPolicy.DomainKey(domainId), SmtpSubmissionMode.RequireAuthentication, Guid.NewGuid(),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("correct-secret"))));
        var otherPolicy = new SmtpSubmissionPolicy(
            SmtpSubmissionPolicy.DomainKey(otherDomainId), SmtpSubmissionMode.RequireAuthentication, Guid.NewGuid(),
            "unused");
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(otherPolicy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherPolicy);
        var cache = new Mock<ISubdomainCache>();
        cache.Setup(x => x.GetSubdomainAsync("allowed.example", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(subdomainId, domainId)));
        cache.Setup(x => x.GetSubdomainAsync("other.example", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(otherSubdomainId, otherDomainId)));
        var services = new ServiceCollection();
        var port = FreePort();
        services.AddSingleton<IOptions<EmailInboxSettings>>(
            Options.Create(new EmailInboxSettings { ImplicitTlsPort = port }));
        services.AddSingleton(session.Object);
        services.AddSingleton(cache.Object);
        services.AddScoped<SmtpSubmissionPolicyService>();
        services.AddTransient<IUserAuthenticator, SpammaUserAuthenticator>();
        services.AddTransient<IMailboxFilter, SmtpSubmissionMailboxFilter>();
        services.AddTransient<IMessageStore, AcceptingMessageStore>();
        using var provider = services.BuildServiceProvider();
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
        using var certificate = X509CertificateLoader.LoadPkcs12(
            generated.Export(X509ContentType.Pfx), null,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);
        var options = new SmtpServerOptionsBuilder()
            .ServerName("Submission test")
            .Endpoint(x => x.Port(port, true).Certificate(certificate).AllowUnsecureAuthentication(false))
            .Build();
        var server = new SmtpServer.SmtpServer(options, provider);
        Exception? serverFault = null;
        server.SessionFaulted += (_, args) => serverFault = args.Exception;
        using var stop = new CancellationTokenSource();
        var serverTask = server.StartAsync(stop.Token);

        try
        {
            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            try
            {
                await ConnectWhenReadyAsync(client, port, serverTask);
            }
            catch (Exception exception) when (serverFault is not null)
            {
                throw new AggregateException("SMTP session faulted during TLS handshake.", exception, serverFault);
            }

            await client.AuthenticateAsync(policy.Username!, "correct-secret");
            var accepted = Message("recipient@allowed.example");
            (await client.SendAsync(accepted)).Should().Be("Ok");

            var wrongTenant = Message("recipient@other.example");
            var rejection = await Assert.ThrowsAsync<SmtpCommandException>(() => client.SendAsync(wrongTenant));
            rejection.StatusCode.Should().Be(SmtpStatusCode.MailboxUnavailable);
            await client.DisconnectAsync(true);

            using var badClient = new SmtpClient();
            badClient.ServerCertificateValidationCallback = (_, _, _, _) => true;
            await badClient.ConnectAsync(IPAddress.Loopback.ToString(), port, SecureSocketOptions.SslOnConnect);
            await Assert.ThrowsAsync<MailKit.Security.AuthenticationException>(
                () => badClient.AuthenticateAsync(policy.Username!, "wrong-secret"));
        }
        finally
        {
            await stop.CancelAsync();
            await serverTask;
        }
    }

    private static MimeMessage Message(string recipient) => new()
    {
        From = { MailboxAddress.Parse("sender@external.example") },
        To = { MailboxAddress.Parse(recipient) },
        Subject = "test",
        Body = new TextPart("plain") { Text = "test" },
    };

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task ConnectWhenReadyAsync(SmtpClient client, int port, Task serverTask)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (serverTask.IsFaulted)
            {
                await serverTask;
            }

            try
            {
                await client.ConnectAsync(IPAddress.Loopback.ToString(), port, SecureSocketOptions.SslOnConnect);
                return;
            }
            catch (SocketException) when (attempt < 19)
            {
                await Task.Delay(100);
            }
        }
    }

    private sealed class AcceptingMessageStore : MessageStore
    {
        public override Task<SmtpServer.Protocol.SmtpResponse> SaveAsync(
            ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken) => Task.FromResult(SmtpServer.Protocol.SmtpResponse.Ok);
    }
}
