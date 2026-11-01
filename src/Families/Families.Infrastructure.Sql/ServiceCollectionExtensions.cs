using Families.Domain;
using Families.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Families.Infrastructure.Sql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFamiliesInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<FamiliesDbContext>(options => options.UseSqlServer(
            connectionString,
            // A free-tier Azure SQL database resumes from auto-pause with transient errors for up to a minute; retries ride that out.
            sql => sql
                .MigrationsHistoryTable("__EFMigrationsHistory", FamiliesDbContext.Schema)
                .EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

        services.AddScoped<IFamilyRepository, FamilyRepository>();

        return services;
    }
}
