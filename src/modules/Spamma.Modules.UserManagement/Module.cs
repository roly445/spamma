using System.Text.Json;
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
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Application.Services;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.JsonConverters;
using Spamma.Modules.UserManagement.Infrastructure.Projections;
using Spamma.Modules.UserManagement.Infrastructure.Repositories;

namespace Spamma.Modules.UserManagement;

[BluQubeResponder]
public static class Module
{
    public static IServiceCollection AddUserManagement(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Module).Assembly);

        services.AddBluQube(typeof(Module).Assembly);
        services.AddBluQubeAuthorization(typeof(Module).Assembly);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasskeyRepository, PasskeyRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddSingleton<IWebAuthnAssertionVerifier, WebAuthnAssertionVerifier>();
        services.AddScoped<Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys.IApiKeyValidationService, Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys.ApiKeyValidationService>();
        services.AddScoped<Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys.IApiKeyRateLimiter, Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys.ApiKeyRateLimiter>();

        services.AddFido2(options =>
        {
            options.ServerDomain = "localhost";
            options.ServerName = "Spamma";
            options.Origins = new HashSet<string> { "http://localhost:5173", "https://localhost:7181" };
        });

        return services;
    }

    public static JsonOptions AddJsonConvertersForUserManagement(this JsonOptions jsonOptions)
    {
        jsonOptions.SerializerOptions.Converters.Add(new ByteArrayJsonConverter());
        jsonOptions.AddBluQubeJsonConverters();
        return jsonOptions;
    }

    public static IEndpointRouteBuilder AddUserManagementApi(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.AddBluQubeApi();
        endpointRouteBuilder.MapGet(
            "api/user-management/api-keys/my",
            async (IQueryRunner queryRunner, HttpContext httpContext) =>
            {
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Unauthorized();
                }

                var result = await queryRunner.Send(new GetMyApiKeysQuery());
                return result.Status == QueryResultStatus.Unauthorized
                    ? Results.Unauthorized()
                    : Results.Json(result);
            });

        return endpointRouteBuilder;
    }

    public static StoreOptions ConfigureUserManagement(this StoreOptions options)
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new ByteArrayJsonConverter());

        options.UseSystemTextJsonForSerialization(jsonOptions);

        options.Projections.Add<UserLookupProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<PasskeyProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<ApiKeyProjection>(ProjectionLifecycle.Inline);
        return options;
    }
}
