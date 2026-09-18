using Microsoft.Extensions.DependencyInjection;
using Shared.Application;

namespace Catalog.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddApplicationLayer(typeof(ServiceCollectionExtensions).Assembly);
    }
}
