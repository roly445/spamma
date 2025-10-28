﻿using BluQube.Attributes;
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
        services.AddTransient<IMessageStore, SpammaMessageStore>();

        // SMTP certificate service
        services.AddSingleton<SmtpCertificateService>();

        // Configure SMTP server with optional TLS port
        services.AddSingleton(provider =>
        {
            var certService = provider.GetRequiredService<SmtpCertificateService>();

            var optionsBuilder = new SmtpServerOptionsBuilder()
                .ServerName("Spamma SMTP Server")
                .Port(25);

            // Add port 587 (STARTTLS) if certificate exists
            var certificate = certService.FindCertificate();
            if (certificate.HasValue)
            {
                optionsBuilder.Port(587);
            }

            return new SmtpServer.SmtpServer(optionsBuilder.Build(), provider.GetRequiredService<IServiceProvider>());
        });

        services.AddHostedService<SmtpHostedService>();
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
        return options;
    }
}