using BluQube.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Spamma.Modules.UserManagement.Client;

[BluQubeRequester]
public static class Module
{
    public static IServiceCollection AddUserManagement(this IServiceCollection services)
    {
        services.AddBluQubeRequesters();

        return services;
    }
}