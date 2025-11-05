using System.Threading.Channels;
using BluQube.Attributes;
using FluentValidation;
using JasperFx.Events.Projections;
using Marten;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.Storage;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.Projections;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox;

[BluQubeResponder]
public static class Module
{
    public static IServiceCollection AddEmailInbox(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Module).Assembly);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Module).Assembly));
        services.AddMediatorAuthorization(typeof(Module).Assembly);
        services.AddAuthorizersFromAssembly(typeof(Module).Assembly);
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddTransient<IMessageStore, SpammaMessageStore>();

        // Background job queues for campaign and chaos address recording (non-blocking operations)
        services.AddSingleton(Channel.CreateUnbounded<Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.CampaignCaptureJob>());
        services.AddSingleton(Channel.CreateUnbounded<Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.ChaosAddressReceivedJob>());
        services.AddHostedService<Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.BackgroundJobProcessor>();

        // CAP integration event handlers
        services.AddScoped<Spamma.Modules.EmailInbox.Infrastructure.IntegrationEventHandlers.PersistReceivedEmailHandler>();

        // Certificate generation service
        services.AddScoped<ICertesLetsEncryptService, CertesLetsEncryptService>();

        // SMTP certificate service
        services.AddSingleton<SmtpCertificateService>();

        // Configure SMTP server with optional TLS port
        services.AddSingleton(provider =>
        {
            var certService = provider.GetRequiredService<SmtpCertificateService>();
            var certificate = certService.FindCertificate();

            var optionsBuilder = new SmtpServerOptionsBuilder()
                .ServerName("Spamma SMTP Server")
                .Endpoint(builder =>
                {
                    builder.Port(25, false);
                });

            // Add port 587 (STARTTLS) with certificate if available
            if (certificate.HasValue)
            {
                optionsBuilder.Endpoint(builder =>
                {
                    builder
                        .Port(587, true)
                        .Certificate(certificate.Value);
                });
            }

            return new SmtpServer.SmtpServer(optionsBuilder.Build(), provider.GetRequiredService<IServiceProvider>());
        });

        services.AddHostedService<SmtpHostedService>();
        services.AddHostedService<EmailCleanupBackgroundService>();
        services.AddSingleton<IMessageStoreProvider, LocalMessageStoreProvider>();
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
        return endpointRouteBuilder;
    }

    public static StoreOptions ConfigureEmailInbox(this StoreOptions options)
    {
        options.Projections.Add<EmailLookupProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<CampaignSummaryProjection>(ProjectionLifecycle.Inline);

        options.Schema.For<CampaignSummary>().Identity(x => x.CampaignId);

        return options;
    }
}