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
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", FamiliesDbContext.Schema)));

        services.AddScoped<IFamilyRepository, FamilyRepository>();

        return services;
    }
}
