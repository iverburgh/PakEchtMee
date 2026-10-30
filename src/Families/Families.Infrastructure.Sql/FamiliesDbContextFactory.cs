using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Families.Infrastructure.Sql;

/// <summary>Only used by the EF Core tooling when generating migrations; the connection string is never opened.</summary>
internal sealed class FamiliesDbContextFactory : IDesignTimeDbContextFactory<FamiliesDbContext>
{
    public FamiliesDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<FamiliesDbContext> options = new();
        options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PakEchtMee.DesignTime;Trusted_Connection=True");

        return new FamiliesDbContext(options.Options);
    }
}
