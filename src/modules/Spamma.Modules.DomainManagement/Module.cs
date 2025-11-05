using BluQube.Attributes;
using DnsClient;
using FluentValidation;
using JasperFx.Events.Projections;
using Marten;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nager.PublicSuffix;
using Spamma.Modules.DomainManagement.Application.Repositories;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using Spamma.Modules.DomainManagement.Infrastructure.IntegrationEventHandlers;
using Spamma.Modules.DomainManagement.Infrastructure.Projections;
using Spamma.Modules.DomainManagement.Infrastructure.Repositories;
using Spamma.Modules.DomainManagement.Infrastructure.Services;
using Spamma.Modules.DomainManagement.Infrastructure.Services.Caching;
using StackExchange.Redis;

namespace Spamma.Modules.DomainManagement;

[BluQubeResponder]
public static class Module
{
    public static IServiceCollection AddDomainManagement(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Module).Assembly);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Module).Assembly));
        services.AddMediatorAuthorization(typeof(Module).Assembly);
        services.AddAuthorizersFromAssembly(typeof(Module).Assembly);
        services.AddScoped<IDomainRepository, DomainRepository>();
        services.AddScoped<ISubdomainRepository, SubdomainRepository>();
        services.AddScoped<IChaosAddressRepository, ChaosAddressRepository>();
        services.AddScoped<ILookupClient, LookupClient>();

        // Register caching services for performance optimization
        // Note: Scoped lifetime (not Singleton) to allow injection of scoped IQuerier for fallback queries
        // Redis connection itself is thread-safe and shared; scoping here only affects the wrapper lifetime
        services.AddScoped<ISubdomainCache, SubdomainCache>();
        services.AddScoped<IChaosAddressCache, ChaosAddressCache>();

        // Register cache invalidation event handler for CAP subscribers
        services.AddScoped<CacheInvalidationEventHandler>();

        // Register domain parser service - will be initialized by hosted service
        services.AddSingleton<IDomainParserService, DomainParserService>();

        return services;
    }

    public static JsonOptions AddJsonConvertersForDomainManagement(this JsonOptions jsonOptions)
    {
        jsonOptions.AddBluQubeJsonConverters();
        return jsonOptions;
    }

    public static IEndpointRouteBuilder AddDomainManagementApi(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.AddBluQubeApi();
        return endpointRouteBuilder;
    }

    public static StoreOptions ConfigureDomainManagement(this StoreOptions options)
    {
        options.Projections.Add<DomainLookupProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<SubdomainLookupProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<ChaosAddressLookupProjection>(ProjectionLifecycle.Inline);
        return options;
    }
}