using BluQube.Attributes;
using BluQube.Authorization;
using FluentValidation;
using JasperFx.Events.Projections;
using Marten;
using Mediator;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Storage;
using Spamma.Modules.EmailInbox.Application.Repositories;
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

        // Configure SMTP server with optional TLS port
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
        services.AddScoped<IEmailInboxSettingsService, EmailInboxSettingsService>();
        services.AddScoped<ICatchAllSenderAddressCache, CatchAllSenderAddressCache>();
        RegisterHandlers(services, typeof(Module).Assembly);
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
        options.Projections.Add<CatchAllSenderAddressLookupProjection>(ProjectionLifecycle.Inline);

        options.Schema.For<EmailLookup>().Identity(x => x.Id);
        options.Schema.For<CatchAllSenderAddressLookup>().Identity(x => x.Id);
        options.Schema.For<CampaignSummary>().Identity(x => x.CampaignId);
        options.Schema.For<EmailInboxSettingsDocument>().Identity(x => x.Id);

        return options;
    }

    private static void RegisterHandlers(IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var requestHandlerType = typeof(IRequestHandler<,>);
        foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface))
        {
            foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestHandlerType))
            {
                services.Add(new ServiceDescriptor(iface, type, ServiceLifetime.Scoped));
            }
        }
    }
}
