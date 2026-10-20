using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;

internal static class SmtpSubmissionApi
{
    internal static IEndpointRouteBuilder MapSmtpSubmissionApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/email-inbox/domains/{domainId:guid}/smtp-submission", GetDomainAsync);
        endpoints.MapPut("api/email-inbox/domains/{domainId:guid}/smtp-submission", SetDomainAsync);
        endpoints.MapGet("api/email-inbox/subdomains/{subdomainId:guid}/smtp-submission", GetSubdomainAsync);
        endpoints.MapPut("api/email-inbox/subdomains/{subdomainId:guid}/smtp-submission", SetSubdomainAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDomainAsync(
        Guid domainId, HttpContext context, IQueryRunner queries,
        SmtpSubmissionPolicyService policies, CancellationToken cancellationToken)
    {
        if (!await CanManageDomainAsync(domainId, context, queries, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var policy = await policies.GetDomainAsync(domainId, cancellationToken);
        return Results.Ok(View(policy, SmtpSubmissionMode.Disabled, policy, null));
    }

    private static async Task<IResult> SetDomainAsync(
        Guid domainId, SmtpSubmissionPolicyRequest request, HttpContext context, IQueryRunner queries,
        SmtpSubmissionPolicyService policies, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!await CanManageDomainAsync(domainId, context, queries, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (request.Mode is not (SmtpSubmissionMode.Disabled or SmtpSubmissionMode.RequireAuthentication))
        {
            return Results.BadRequest("Choose Disabled or RequireAuthentication for a domain.");
        }

        var (policy, secret) = await policies.SetAsync(SmtpSubmissionPolicy.DomainKey(domainId), request.Mode, cancellationToken);
        return Results.Ok(View(policy, SmtpSubmissionMode.Disabled, policy, secret));
    }

    private static async Task<IResult> GetSubdomainAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries,
        SmtpSubmissionPolicyService policies, CancellationToken cancellationToken)
    {
        var subdomain = await GetManagedSubdomainAsync(subdomainId, context, queries, cancellationToken);
        if (subdomain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var configured = await policies.GetSubdomainAsync(subdomainId, cancellationToken);
        var effective = await policies.GetEffectiveAsync(subdomain.DomainId, subdomainId, cancellationToken);
        return Results.Ok(View(configured, SmtpSubmissionMode.Inherit, effective, null));
    }

    private static async Task<IResult> SetSubdomainAsync(
        Guid subdomainId, SmtpSubmissionPolicyRequest request, HttpContext context, IQueryRunner queries,
        SmtpSubmissionPolicyService policies, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var subdomain = await GetManagedSubdomainAsync(subdomainId, context, queries, cancellationToken);
        if (subdomain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!Enum.IsDefined(request.Mode))
        {
            return Results.BadRequest("Choose Inherit, Disabled, or RequireAuthentication.");
        }

        var (configured, secret) = await policies.SetAsync(SmtpSubmissionPolicy.SubdomainKey(subdomainId), request.Mode, cancellationToken);
        var effective = await policies.GetEffectiveAsync(subdomain.DomainId, subdomainId, cancellationToken);
        return Results.Ok(View(configured, SmtpSubmissionMode.Inherit, effective, secret));
    }

    private static async Task<bool> CanManageDomainAsync(
        Guid domainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken)
    {
        if (!context.ToUserAuthInfo().IsAuthenticated)
        {
            return false;
        }

        var result = await queries.Send(new GetDetailedDomainByIdQuery(domainId), cancellationToken);
        return result.Status == QueryResultStatus.Succeeded && !result.Data.IsSuspended;
    }

    private static async Task<GetDetailedSubdomainByIdQueryResult?> GetManagedSubdomainAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken)
    {
        if (!context.ToUserAuthInfo().IsAuthenticated)
        {
            return null;
        }

        var result = await queries.Send(new GetDetailedSubdomainByIdQuery(subdomainId), cancellationToken);
        return result.Status == QueryResultStatus.Succeeded && result.Data.Status != DomainManagement.Client.Contracts.SubdomainStatus.Suspended
            ? result.Data
            : null;
    }

    private static SmtpSubmissionPolicyView View(
        SmtpSubmissionPolicy? configured, SmtpSubmissionMode defaultMode,
        SmtpSubmissionPolicy? effective, string? secret) => new(
            configured?.Mode ?? defaultMode,
            effective?.Mode ?? SmtpSubmissionMode.Disabled,
            effective?.Id,
            effective?.Username,
            secret);
}
