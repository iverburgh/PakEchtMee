using Microsoft.Extensions.DependencyInjection;
using Shared.Application;

namespace Families.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFamiliesApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddApplicationLayer(typeof(ServiceCollectionExtensions).Assembly);
    }
}
