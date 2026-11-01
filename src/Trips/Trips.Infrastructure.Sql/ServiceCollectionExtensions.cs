using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trips.Domain;
using Trips.Infrastructure.Sql.Repositories;
using Trips.Infrastructure.Sql.Seeding;

namespace Trips.Infrastructure.Sql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTripsInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TripsDbContext>(options => options.UseSqlServer(
            connectionString,
            // A free-tier Azure SQL database resumes from auto-pause with transient errors for up to a minute; retries ride that out.
            sql => sql
                .MigrationsHistoryTable("__EFMigrationsHistory", TripsDbContext.Schema)
                .EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<TripsSeeder>();

        return services;
    }
}
