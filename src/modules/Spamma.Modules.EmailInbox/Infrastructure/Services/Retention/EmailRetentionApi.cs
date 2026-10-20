using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;

internal static class EmailRetentionApi
{
    internal static IEndpointRouteBuilder MapEmailRetentionApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/email-inbox/domains/{domainId:guid}/retention", GetDomainAsync);
        endpoints.MapPut("api/email-inbox/domains/{domainId:guid}/retention", SetDomainAsync);
        endpoints.MapGet("api/email-inbox/subdomains/{subdomainId:guid}/retention", GetSubdomainAsync);
        endpoints.MapPut("api/email-inbox/subdomains/{subdomainId:guid}/retention", SetSubdomainAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDomainAsync(
        Guid domainId, HttpContext context, IQueryRunner queries,
        EmailRetentionPolicyService policies, CancellationToken cancellationToken)
    {
        var domain = await GetManagedDomainAsync(domainId, context, queries, cancellationToken);
        if (domain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var policy = await policies.GetDomainAsync(domainId, cancellationToken);
        return Results.Ok(View(policy, policy, domain.DomainName));
    }

    private static async Task<IResult> SetDomainAsync(
        Guid domainId, EmailRetentionPolicyRequest request, HttpContext context, IQueryRunner queries,
        EmailRetentionPolicyService policies, CancellationToken cancellationToken)
    {
        var domain = await GetManagedDomainAsync(domainId, context, queries, cancellationToken);
        if (domain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (request.Mode != EmailRetentionMode.Override || !ValidLimits(request.MaxAgeHours, request.MaxMessages))
        {
            return Results.BadRequest("Choose valid limits for the domain.");
        }

        var policy = await policies.SetDomainAsync(domainId, request.MaxAgeHours, request.MaxMessages, cancellationToken);
        return Results.Ok(View(policy, policy, domain.DomainName));
    }

    private static async Task<IResult> GetSubdomainAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries,
        EmailRetentionPolicyService policies, CancellationToken cancellationToken)
    {
        var subdomain = await GetManagedSubdomainAsync(subdomainId, context, queries, cancellationToken);
        if (subdomain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var configured = await policies.GetSubdomainAsync(subdomainId, cancellationToken);
        var effective = await policies.GetEffectiveAsync(subdomain.DomainId, subdomainId, cancellationToken);
        return Results.Ok(View(configured, effective, subdomain.FullName, subdomain.DomainName));
    }

    private static async Task<IResult> SetSubdomainAsync(
        Guid subdomainId, EmailRetentionPolicyRequest request, HttpContext context, IQueryRunner queries,
        EmailRetentionPolicyService policies, CancellationToken cancellationToken)
    {
        var subdomain = await GetManagedSubdomainAsync(subdomainId, context, queries, cancellationToken);
        if (subdomain is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!Enum.IsDefined(request.Mode) ||
            (request.Mode == EmailRetentionMode.Override && !ValidLimits(request.MaxAgeHours, request.MaxMessages)))
        {
            return Results.BadRequest("Choose valid retention limits or inherit the domain policy.");
        }

        var configured = await policies.SetSubdomainAsync(subdomainId, request.Mode,
            request.MaxAgeHours, request.MaxMessages, cancellationToken);
        var effective = await policies.GetEffectiveAsync(subdomain.DomainId, subdomainId, cancellationToken);
        return Results.Ok(View(configured, effective, subdomain.FullName, subdomain.DomainName));
    }

    private static bool ValidLimits(int? maxAgeHours, int? maxMessages) =>
        (maxAgeHours is null or >= 1 and <= 87600) &&
        (maxMessages is null or >= 1 and <= 1000000);

    private static async Task<GetDetailedDomainByIdQueryResult?> GetManagedDomainAsync(
        Guid domainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken)
    {
        if (!context.ToUserAuthInfo().IsAuthenticated)
        {
            return null;
        }

        var result = await queries.Send(new GetDetailedDomainByIdQuery(domainId), cancellationToken);
        return result.Status == QueryResultStatus.Succeeded && !result.Data.IsSuspended ? result.Data : null;
    }

    private static async Task<GetDetailedSubdomainByIdQueryResult?> GetManagedSubdomainAsync(
        Guid subdomainId, HttpContext context, IQueryRunner queries, CancellationToken cancellationToken)
    {
        if (!context.ToUserAuthInfo().IsAuthenticated)
        {
            return null;
        }

        var result = await queries.Send(new GetDetailedSubdomainByIdQuery(subdomainId), cancellationToken);
        return result.Status == QueryResultStatus.Succeeded &&
               result.Data.Status != DomainManagement.Client.Contracts.SubdomainStatus.Suspended
            ? result.Data
            : null;
    }

    private static EmailRetentionPolicyView View(EmailRetentionPolicy configured, EmailRetentionPolicy effective,
        string scopeName, string? parentDomainName = null) => new(
        configured.Mode,
        configured.MaxAgeHours,
        configured.MaxMessages,
        effective.MaxAgeHours,
        effective.MaxMessages,
        scopeName,
        parentDomainName);
}
