using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trips.Infrastructure.Sql;

/// <summary>Only used by the EF Core tooling when generating migrations; the connection string is never opened.</summary>
internal sealed class TripsDbContextFactory : IDesignTimeDbContextFactory<TripsDbContext>
{
    public TripsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<TripsDbContext> options = new();
        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PakEchtMee.DesignTime;Trusted_Connection=True");

        return new TripsDbContext(options.Options);
    }
}
