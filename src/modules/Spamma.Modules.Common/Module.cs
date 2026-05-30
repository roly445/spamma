using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.Common.Application.Behaviors;

namespace Spamma.Modules.Common;

public static class Module
{
    public static IServiceCollection AddCommonBehaviors(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(CommandQueryTracingBehavior<,>));
        return services;
    }
}
