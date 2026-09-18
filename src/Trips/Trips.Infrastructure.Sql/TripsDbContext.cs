using Microsoft.EntityFrameworkCore;
using Trips.Domain;

namespace Trips.Infrastructure.Sql;

public sealed class TripsDbContext(DbContextOptions<TripsDbContext> options) : DbContext(options)
{
    public const string Schema = "trips";

    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
