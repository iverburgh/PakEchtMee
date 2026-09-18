using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Sql;

/// <summary>Only used by the EF Core tooling when generating migrations; the connection string is never opened.</summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<CatalogDbContext> options = new();
        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PakEchtMee.DesignTime;Trusted_Connection=True");

        return new CatalogDbContext(options.Options);
    }
}
