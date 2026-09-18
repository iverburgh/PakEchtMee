using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Application.Behaviors;

namespace Shared.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the MediatR handlers of the given assemblies together with the shared pipeline behaviours.</summary>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length is 0)
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(assemblies);
            configuration.AddOpenBehavior(typeof(TracingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ExceptionToResultBehavior<,>));
        });

        return services;
    }
}
