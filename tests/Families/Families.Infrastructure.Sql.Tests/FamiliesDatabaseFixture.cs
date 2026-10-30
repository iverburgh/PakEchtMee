using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerTestContainerFixture))]

namespace Families.Infrastructure.Sql.Tests;

/// <summary>Gives the test class its own migrated database on the shared container.</summary>
public sealed class FamiliesDatabaseFixture(SqlServerTestContainerFixture server) : IAsyncLifetime
{
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"families_{Guid.NewGuid():N}");

        await using FamiliesDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FamiliesDbContext CreateContext()
        => new(new DbContextOptionsBuilder<FamiliesDbContext>().UseSqlServer(connectionString).Options);
}
