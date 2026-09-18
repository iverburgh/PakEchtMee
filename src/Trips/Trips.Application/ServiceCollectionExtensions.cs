using Microsoft.Extensions.DependencyInjection;
using Shared.Application;

namespace Trips.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTripsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddApplicationLayer(typeof(ServiceCollectionExtensions).Assembly);
    }
}
