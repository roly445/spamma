using BluQube.Attributes;
using FluentValidation;
using JasperFx.Events.Projections;
using Marten;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Infrastructure.Projections;
using Spamma.Modules.UserManagement.Infrastructure.Repositories;

namespace Spamma.Modules.UserManagement;

[BluQubeResponder]
public static class Module
{
    public static IServiceCollection AddUserManagement(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Module).Assembly);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Module).Assembly));
        services.AddMediatorAuthorization(typeof(Module).Assembly);
        services.AddAuthorizersFromAssembly(typeof(Module).Assembly);
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }

    public static JsonOptions AddJsonConvertersForUserManagement(this JsonOptions jsonOptions)
    {
        return jsonOptions;
    }

    public static IEndpointRouteBuilder AddUserManagementApi(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.AddBluQubeApi();
        return endpointRouteBuilder;
    }

    public static StoreOptions ConfigureUserManagement(this StoreOptions options)
    {
        options.Projections.Add<UserLookupProjection>(ProjectionLifecycle.Inline);
        return options;
    }
}