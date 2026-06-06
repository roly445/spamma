using System.IO.Compression;
using BluQube.Attributes;
using BluQube.Authorization;
using BluQube.Constants;
using BluQube.Queries;
using FluentValidation;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Storage;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.Projections;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;

namespace Spamma.Modules.EmailInbox;

[BluQubeResponder]
public static class Module
{
    public static IServiceCollection AddEmailInbox(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Module).Assembly);

        services.AddBluQube(typeof(Module).Assembly);
        services.AddBluQubeAuthorization(typeof(Module).Assembly);
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICatchAllSenderAddressRepository, CatchAllSenderAddressRepository>();
        services.AddSingleton<PushNotificationManager>();
        services.AddScoped<EmailPushGrpcService>();
        services.AddTransient<IMessageStore, SpammaMessageStore>();

        // Background job queues
        services.AddHostedService<BackgroundTaskService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Certificate generation services
        services.AddScoped<ICertesLetsEncryptService, CertesLetsEncryptService>();
        services.AddSingleton<ISelfSignedCertificateService, SelfSignedCertificateService>();

        // SMTP certificate service
        services.AddSingleton<SmtpCertificateService>();

        // Configure SMTP server with optional TLS endpoints
        services.AddSingleton(provider =>
        {
            var smtpSettings = provider.GetRequiredService<IOptions<EmailInboxSettings>>().Value;
            var certService = provider.GetRequiredService<SmtpCertificateService>();
            var certificate = certService.FindCertificate();

            var optionsBuilder = new SmtpServerOptionsBuilder()
                .ServerName("Spamma SMTP Server")
                .Endpoint(builder =>
                {
                    builder.Port(smtpSettings.Port, false);
                });

            // Add standard TLS endpoints with certificate if available.
            // Port 465 uses implicit TLS; port 587 advertises STARTTLS on a plain connection.
            if (certificate.HasValue)
            {
                optionsBuilder.Endpoint(builder =>
                {
                    builder
                        .Port(465, true)
                        .Certificate(certificate.Value);
                });

                optionsBuilder.Endpoint(builder =>
                {
                    builder
                        .Port(587, false)
                        .Certificate(certificate.Value);
                });
            }

            return new SmtpServer.SmtpServer(optionsBuilder.Build(), provider.GetRequiredService<IServiceProvider>());
        });

        services.AddHostedService<SmtpHostedService>();
        services.AddHostedService<EmailCleanupBackgroundService>();
        services.AddSingleton<IMessageStoreProvider, LocalMessageStoreProvider>();
        services.AddScoped<IEmailInboxSettingsService, EmailInboxSettingsService>();
        services.AddScoped<ICatchAllSenderAddressCache, CatchAllSenderAddressCache>();
        return services;
    }

    public static JsonOptions AddJsonConvertersForEmailInbox(this JsonOptions jsonOptions)
    {
        jsonOptions.AddBluQubeJsonConverters();
        return jsonOptions;
    }

    public static IEndpointRouteBuilder AddEmailInboxApi(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.AddBluQubeApi();
        endpointRouteBuilder.MapGet(
            "email-inbox/get-email-mime-message-by-id",
            async (IQueryRunner queryRunner, HttpContext httpContext, Guid emailId) =>
            {
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Unauthorized();
                }

                var result = await queryRunner.Send(new GetEmailMimeMessageByIdQuery(emailId));
                return result.Status == QueryResultStatus.Unauthorized
                    ? Results.Unauthorized()
                    : Results.Json(result);
            });

        endpointRouteBuilder.MapGet(
            "api/email-inbox/emails/{emailId:guid}/mime-content",
            async (IDocumentSession documentSession, IMessageStoreProvider messageStoreProvider, HttpContext httpContext, Guid emailId, CancellationToken cancellationToken) =>
            {
                var user = httpContext.ToUserAuthInfo();
                if (!user.IsAuthenticated)
                {
                    return Results.Unauthorized();
                }

                var email = await documentSession.LoadAsync<EmailLookup>(emailId, cancellationToken);
                if (email is null || !CanAccessEmail(user, email))
                {
                    return Results.Unauthorized();
                }

                var message = await messageStoreProvider.LoadMessageContentAsync(emailId, cancellationToken);
                if (message.HasNoValue)
                {
                    return Results.NotFound();
                }

                var content = await CompressMimeMessage(message.Value, cancellationToken);
                return Results.File(content, "application/gzip");
            });

        return endpointRouteBuilder;
    }

    public static StoreOptions ConfigureEmailInbox(this StoreOptions options)
    {
        options.Projections.Add<EmailLookupProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<CampaignSummaryProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<CatchAllSenderAddressLookupProjection>(ProjectionLifecycle.Inline);

        options.Schema.For<EmailLookup>().Identity(x => x.Id);
        options.Schema.For<CatchAllSenderAddressLookup>().Identity(x => x.Id);
        options.Schema.For<CampaignSummary>().Identity(x => x.CampaignId);
        options.Schema.For<EmailInboxSettingsDocument>().Identity(x => x.Id);

        return options;
    }

    private static bool CanAccessEmail(UserAuthInfo user, EmailLookup email)
    {
        return user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
               user.ModeratedDomains.Contains(email.DomainId) ||
               user.ModeratedSubdomains.Contains(email.SubdomainId) ||
               user.ViewableSubdomains.Contains(email.SubdomainId);
    }

    private static async Task<byte[]> CompressMimeMessage(MimeKit.MimeMessage message, CancellationToken cancellationToken)
    {
        await using var messageStream = new MemoryStream();
        await message.WriteToAsync(messageStream, cancellationToken);
        messageStream.Seek(0, SeekOrigin.Begin);

        await using var outputStream = new MemoryStream();
        await using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await messageStream.CopyToAsync(gzip, cancellationToken);
        }

        return outputStream.ToArray();
    }
}
