using System.Security.Cryptography;
using BluQube.Constants;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

internal static class SpamFeedbackApi
{
    internal static IEndpointRouteBuilder MapSpamFeedbackApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/email-inbox/subdomains/{subdomainId:guid}/spam-feedback", GetSettingsAsync);
        endpoints.MapPut("api/email-inbox/subdomains/{subdomainId:guid}/spam-feedback", SaveSettingsAsync);
        endpoints.MapGet("api/email-inbox/emails/{emailId:guid}/spam-report", GetReportAsync);
        endpoints.MapPost("api/email-inbox/emails/{emailId:guid}/spam-report", CreateReportAsync);
        endpoints.MapPost("api/email-inbox/emails/{emailId:guid}/spam-report/{channel}/retry", RetryAsync);
        return endpoints;
    }

    private static async Task<IResult> GetSettingsAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries, IDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (!await CanConfigureAsync(subdomainId, context, queries, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var settings = await session.LoadAsync<SpamFeedbackConfiguration>(subdomainId, cancellationToken);
        return Results.Ok(new SpamFeedbackSettingsView(
            settings?.ArfRecipient, settings?.WebhookUrl,
            !string.IsNullOrWhiteSpace(settings?.ProtectedWebhookSecret)));
    }

    private static async Task<IResult> SaveSettingsAsync(
        Guid subdomainId, SpamFeedbackSettingsRequest request, HttpContext context, IQueryRunner queries,
        IDocumentSession session, IDataProtectionProvider protectionProvider, CancellationToken cancellationToken)
    {
        var subdomain = await GetConfigurableSubdomainAsync(subdomainId, context, queries, cancellationToken);
        if (subdomain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var arfRecipient = request.ArfRecipient?.Trim();
        if (string.IsNullOrWhiteSpace(arfRecipient))
        {
            arfRecipient = null;
        }
        else if (!MimeKit.MailboxAddress.TryParse(arfRecipient, out var mailbox) ||
                 !mailbox.Domain.Equals(subdomain.DomainName, StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.ArfRecipient)] = ["Use an email address on the verified parent domain."],
            });
        }

        var webhookUrl = request.WebhookUrl?.Trim();
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            webhookUrl = null;
        }
        else if (!WebhookDestinationPolicy.IsAllowedUrl(webhookUrl) ||
                 !WebhookDestinationPolicy.IsUnderParentDomain(webhookUrl, subdomain.DomainName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.WebhookUrl)] = ["Use a public HTTPS URL on the verified parent domain."],
            });
        }

        var previous = await session.LoadAsync<SpamFeedbackConfiguration>(subdomainId, cancellationToken);
        string? newSecret = null;
        string? protectedSecret = null;
        if (webhookUrl is not null)
        {
            if (previous?.WebhookUrl == webhookUrl && !string.IsNullOrWhiteSpace(previous.ProtectedWebhookSecret))
            {
                protectedSecret = previous.ProtectedWebhookSecret;
            }
            else
            {
                newSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                protectedSecret = protectionProvider.CreateProtector("Spamma.SpamFeedback.WebhookSecret.v1")
                    .Protect(newSecret);
            }
        }

        session.Store(new SpamFeedbackConfiguration(subdomainId, arfRecipient, webhookUrl, protectedSecret));
        await session.SaveChangesAsync(cancellationToken);
        return Results.Ok(new SpamFeedbackSettingsView(arfRecipient, webhookUrl, protectedSecret is not null, newSecret));
    }

    private static async Task<IResult> GetReportAsync(
        Guid emailId, HttpContext context, IDocumentSession session, CancellationToken cancellationToken)
    {
        if (!await CanAccessEmailAsync(emailId, context, session, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var report = await session.LoadAsync<SpamReport>(emailId, cancellationToken);
        return report is null ? Results.NotFound() : Results.Ok(SpamReportView.From(report));
    }

    private static async Task<IResult> CreateReportAsync(
        Guid emailId, HttpContext context, IDocumentSession session, SpamReportService reports,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessEmailAsync(emailId, context, session, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var report = await reports.CreateAsync(emailId, SpamReportTrigger.Manual, cancellationToken);
        return report is null ? Results.NotFound() : Results.Ok(SpamReportView.From(report));
    }

    private static async Task<IResult> RetryAsync(
        Guid emailId, string channel, HttpContext context, IDocumentSession session,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!await CanAccessEmailAsync(emailId, context, session, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var report = await session.LoadAsync<SpamReport>(emailId, cancellationToken);
        if (report is null)
        {
            return Results.NotFound();
        }

        report = channel.ToLowerInvariant() switch
        {
            "email" when report.EmailDelivery.Status == FeedbackDeliveryStatus.Failed =>
                report with
                {
                    EmailDelivery = report.EmailDelivery with
                    {
                        Status = FeedbackDeliveryStatus.Pending,
                        LastError = null,
                        NextAttemptAt = clock.GetUtcNow(),
                    },
                    NextAttemptAt = clock.GetUtcNow(),
                },
            "webhook" when report.WebhookDelivery.Status == FeedbackDeliveryStatus.Failed =>
                report with
                {
                    WebhookDelivery = report.WebhookDelivery with
                    {
                        Status = FeedbackDeliveryStatus.Pending,
                        LastError = null,
                        NextAttemptAt = clock.GetUtcNow(),
                    },
                    NextAttemptAt = clock.GetUtcNow(),
                },
            _ => null,
        };
        if (report is null)
        {
            return Results.BadRequest("Only a failed, enabled channel can be retried.");
        }

        session.Store(report);
        await session.SaveChangesAsync(cancellationToken);
        return Results.Ok(SpamReportView.From(report));
    }

    private static async Task<bool> CanAccessEmailAsync(
        Guid emailId, HttpContext context, IDocumentSession session, CancellationToken cancellationToken)
    {
        var user = context.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return false;
        }

        var email = await session.LoadAsync<EmailLookup>(emailId, cancellationToken);
        return email is not null && await EmailAccessAuthorizer.CanAccessAsync(user, email, session, cancellationToken);
    }

    private static async Task<bool> CanConfigureAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken) =>
        await GetConfigurableSubdomainAsync(subdomainId, context, queries, cancellationToken) is not null;

    private static async Task<GetDetailedSubdomainByIdQueryResult?> GetConfigurableSubdomainAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken)
    {
        var user = context.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return null;
        }

        var result = await queries.Send(new GetDetailedSubdomainByIdQuery(subdomainId), cancellationToken);
        if (result.Status != QueryResultStatus.Succeeded)
        {
            return null;
        }

        return user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
               user.ModeratedDomains.Contains(result.Data.DomainId) ||
               user.ModeratedSubdomains.Contains(subdomainId)
            ? result.Data
            : null;
    }
}
