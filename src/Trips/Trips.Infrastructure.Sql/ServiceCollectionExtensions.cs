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
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", TripsDbContext.Schema)));

        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<TripsSeeder>();

        return services;
    }
}
