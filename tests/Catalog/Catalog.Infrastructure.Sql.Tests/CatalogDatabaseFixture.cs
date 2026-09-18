using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerTestContainerFixture))]

namespace Catalog.Infrastructure.Sql.Tests;

/// <summary>Gives the test class its own migrated database on the shared container.</summary>
public sealed class CatalogDatabaseFixture(SqlServerTestContainerFixture server) : IAsyncLifetime
{
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"catalog_{Guid.NewGuid():N}");

        await using CatalogDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public CatalogDbContext CreateContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
}
