using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Repositories;
using Catalog.Infrastructure.Sql.Seeding;
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
            // A free-tier Azure SQL database resumes from auto-pause with transient errors for up to a minute; retries ride that out.
            sql => sql
                .MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema)
                .EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICatalogItemRepository, CatalogItemRepository>();
        services.AddScoped<CatalogSeeder>();

        return services;
    }
}
