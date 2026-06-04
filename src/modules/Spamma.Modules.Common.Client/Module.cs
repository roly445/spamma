using BluQube.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Spamma.Modules.Common.Client;

[BluQubeRequester]
public static class Module
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.AddBluQubeRequesters();

        return services;
    }
}
