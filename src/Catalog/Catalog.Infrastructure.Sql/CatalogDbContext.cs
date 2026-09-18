using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Sql;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CatalogItem> Items => Set<CatalogItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
