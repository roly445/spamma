using BluQube.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Spamma.Modules.EmailInbox.Client;

[BluQubeRequester]
public static class Module
{
    public static IServiceCollection AddEmailInbox(this IServiceCollection services)
    {
        services.AddBluQubeRequesters();

        return services;
    }
}