using BluQube.Mediation;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.Common.Application.Behaviors;

namespace Spamma.Modules.Common;

public static class Module
{
    public static IServiceCollection AddCommonBehaviors(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IBluQubePipelineBehavior<,>), typeof(CommandQueryTracingBehavior<,>));
        return services;
    }
}
