using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Sql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema)));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICatalogItemRepository, CatalogItemRepository>();

        return services;
    }
}
