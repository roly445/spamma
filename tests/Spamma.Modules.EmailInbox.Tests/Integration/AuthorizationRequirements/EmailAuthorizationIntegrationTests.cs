using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration.AuthorizationRequirements;

public class EmailAuthorizationIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private Guid _assignedUserId;
    private Guid _senderAddressId;
    private Guid _catchAllEmailId;

    public EmailAuthorizationIntegrationTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        this._assignedUserId = Guid.NewGuid();
        this._senderAddressId = Guid.NewGuid();
        this._catchAllEmailId = Guid.NewGuid();

        this._fixture.Session!.Store(new CatchAllSenderAddressLookup
        {
            Id = this._senderAddressId,
            SenderAddress = "sender@example.com",
            AssignedUserIds = [this._assignedUserId],
            IsRemoved = false,
            AddedAt = DateTimeOffset.UtcNow,
        });

        this._fixture.Session.Store(new EmailLookup
        {
            Id = this._catchAllEmailId,
            DomainId = Guid.NewGuid(),
            SubdomainId = EmailInboxSettingsDocument.CatchAllSubdomainId,
            Subject = "Catch-all test message",
            SentAt = DateTimeOffset.UtcNow,
            IsFavorite = false,
            CatchAllSenderAddressId = this._senderAddressId,
            EmailAddresses =
            [
                new EmailAddress("sender@example.com", "Sender", Client.Contracts.EmailAddressType.From),
                new EmailAddress("catch-all@example.com", "Recipient", Client.Contracts.EmailAddressType.To),
            ],
        });

        await this._fixture.Session.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetEmailById_WhenUserIsAssignedToCatchAllSender_Succeeds()
    {
        var authorizer = new GetEmailByIdQueryAuthorizer(
            CreateHttpContextAccessor(this._assignedUserId),
            this._fixture.Session!);

        var result = await authorizer.Authorize(new GetEmailByIdQuery(this._catchAllEmailId), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmailMimeMessageById_WhenUserIsAssignedToCatchAllSender_Succeeds()
    {
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer(
            CreateHttpContextAccessor(this._assignedUserId),
            this._fixture.Session!);

        var result = await authorizer.Authorize(new GetEmailMimeMessageByIdQuery(this._catchAllEmailId), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmailMimeMessageById_WhenUserIsNotAssignedToCatchAllSender_Fails()
    {
        var authorizer = new GetEmailMimeMessageByIdQueryAuthorizer(
            CreateHttpContextAccessor(Guid.NewGuid()),
            this._fixture.Session!);

        var result = await authorizer.Authorize(new GetEmailMimeMessageByIdQuery(this._catchAllEmailId), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    private static HttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com"),
        };

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")),
            },
        };
    }
}
