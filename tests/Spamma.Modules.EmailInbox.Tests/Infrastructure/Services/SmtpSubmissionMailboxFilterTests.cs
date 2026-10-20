using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Marten;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmtpServer;
using SmtpServer.IO;
using SmtpServer.Mail;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SmtpSubmissionMailboxFilterTests
{
    [Fact]
    public async Task AuthenticatorAcceptsCurrentSecretOnlyOverTlsSubmission()
    {
        var id = SmtpSubmissionPolicy.DomainKey(Guid.NewGuid());
        var policy = new SmtpSubmissionPolicy(
            id, SmtpSubmissionMode.RequireAuthentication, Guid.NewGuid(),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("current-secret"))));
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        var provider = Provider(session.Object, Mock.Of<ISubdomainCache>());
        var authenticator = new SpammaUserAuthenticator();

        (await authenticator.AuthenticateAsync(
            Context(provider, 587, true, null), policy.Username!, "current-secret", CancellationToken.None)).Should().BeTrue();
        (await authenticator.AuthenticateAsync(
            Context(provider, 587, true, null), policy.Username!, "wrong", CancellationToken.None)).Should().BeFalse();
        (await authenticator.AuthenticateAsync(
            Context(provider, 587, false, null), policy.Username!, "current-secret", CancellationToken.None)).Should().BeFalse();
        (await authenticator.AuthenticateAsync(
            Context(provider, 25, true, null), policy.Username!, "current-secret", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task SubmissionRequiresEffectiveCredentialForEveryRecipient()
    {
        var domainId = Guid.NewGuid();
        var inheritedId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var parent = Policy(SmtpSubmissionPolicy.DomainKey(domainId));
        var child = Policy(SmtpSubmissionPolicy.SubdomainKey(overrideId));
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(parent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(child.Id, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        var cache = new Mock<ISubdomainCache>();
        cache.Setup(x => x.GetSubdomainAsync("inherited.example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(inheritedId, domainId)));
        cache.Setup(x => x.GetSubdomainAsync("override.example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(overrideId, domainId)));
        var provider = Provider(session.Object, cache.Object);
        var filter = new SmtpSubmissionMailboxFilter();
        var parentContext = Context(provider, 587, true, parent.Username);
        var childContext = Context(provider, 587, true, child.Username);
        var sender = new Mailbox("sender@external.example");

        (await filter.CanDeliverToAsync(parentContext, new Mailbox("a@inherited.example.com"), sender, CancellationToken.None))
            .Should().BeTrue();
        (await filter.CanDeliverToAsync(parentContext, new Mailbox("b@override.example.com"), sender, CancellationToken.None))
            .Should().BeFalse();
        (await filter.CanDeliverToAsync(childContext, new Mailbox("b@override.example.com"), sender, CancellationToken.None))
            .Should().BeTrue();
        (await filter.CanDeliverToAsync(Context(provider, 587, true, null), new Mailbox("a@inherited.example.com"), sender, CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact]
    public async Task DisabledSubdomainAllowsAnonymousTlsSubmissionButNotPlaintext()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var child = new SmtpSubmissionPolicy(
            SmtpSubmissionPolicy.SubdomainKey(subdomainId), SmtpSubmissionMode.Disabled, Guid.Empty, null);
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(child.Id, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        var cache = new Mock<ISubdomainCache>();
        cache.Setup(x => x.GetSubdomainAsync("open.example.com", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(subdomainId, domainId)));
        var provider = Provider(session.Object, cache.Object);
        var filter = new SmtpSubmissionMailboxFilter();
        var recipient = new Mailbox("a@open.example.com");
        var sender = new Mailbox("sender@external.example");

        (await filter.CanDeliverToAsync(Context(provider, 587, true, null), recipient, sender, CancellationToken.None))
            .Should().BeTrue();
        (await filter.CanDeliverToAsync(Context(provider, 587, false, null), recipient, sender, CancellationToken.None))
            .Should().BeFalse();
        (await filter.CanDeliverToAsync(Context(provider, 25, false, null), recipient, sender, CancellationToken.None))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RejectedRecipientMarksWholeTransactionUntilNextMailFrom()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var policy = Policy(SmtpSubmissionPolicy.DomainKey(domainId));
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        var cache = new Mock<ISubdomainCache>();
        cache.Setup(x => x.GetSubdomainAsync("allowed.example", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(new ISubdomainCache.CachedSubdomain(subdomainId, domainId)));
        cache.Setup(x => x.GetSubdomainAsync("unknown.example", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ISubdomainCache.CachedSubdomain>.Nothing);
        var provider = Provider(session.Object, cache.Object);
        var context = Context(provider, 587, true, policy.Username);
        var filter = new SmtpSubmissionMailboxFilter();
        var sender = new Mailbox("sender@external.example");

        (await filter.CanDeliverToAsync(context, new Mailbox("x@unknown.example"), sender, CancellationToken.None))
            .Should().BeFalse();
        (await filter.CanDeliverToAsync(context, new Mailbox("x@allowed.example"), sender, CancellationToken.None))
            .Should().BeTrue();
        SmtpSubmissionMailboxFilter.HadRejectedRecipient(context).Should().BeTrue();
        await filter.CanAcceptFromAsync(context, sender, 0, CancellationToken.None);
        SmtpSubmissionMailboxFilter.HadRejectedRecipient(context).Should().BeFalse();
    }

    private static SmtpSubmissionPolicy Policy(string id) =>
        new(id, SmtpSubmissionMode.RequireAuthentication, Guid.NewGuid(), "unused");

    private static IServiceProvider Provider(IDocumentSession session, ISubdomainCache cache)
    {
        var services = new ServiceCollection();
        services.AddSingleton(session);
        services.AddSingleton(cache);
        services.AddScoped<SmtpSubmissionPolicyService>();
        return services.BuildServiceProvider();
    }

    private static ISessionContext Context(IServiceProvider provider, int port, bool secure, string? username)
    {
        var endpoint = new Mock<IEndpointDefinition>();
        endpoint.Setup(x => x.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, port));
        var pipe = new Mock<ISecurableDuplexPipe>();
        pipe.Setup(x => x.IsSecure).Returns(secure);
        var context = new Mock<ISessionContext>();
        context.Setup(x => x.ServiceProvider).Returns(provider);
        context.Setup(x => x.EndpointDefinition).Returns(endpoint.Object);
        context.Setup(x => x.Pipe).Returns(pipe.Object);
        context.Setup(x => x.Properties).Returns(new Dictionary<string, object>());
        context.Setup(x => x.Authentication).Returns(username is null
            ? AuthenticationContext.Unauthenticated
            : new AuthenticationContext(username));
        return context.Object;
    }
}
