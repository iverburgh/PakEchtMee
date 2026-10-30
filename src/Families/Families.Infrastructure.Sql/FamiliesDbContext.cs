using Families.Domain;
using Microsoft.EntityFrameworkCore;

namespace Families.Infrastructure.Sql;

public sealed class FamiliesDbContext(DbContextOptions<FamiliesDbContext> options) : DbContext(options)
{
    public const string Schema = "families";

    public DbSet<Family> Families => Set<Family>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamiliesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
