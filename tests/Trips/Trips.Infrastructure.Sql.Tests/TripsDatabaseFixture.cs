using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerTestContainerFixture))]

namespace Trips.Infrastructure.Sql.Tests;

/// <summary>Gives the test class its own migrated database on the shared container.</summary>
public sealed class TripsDatabaseFixture(SqlServerTestContainerFixture server) : IAsyncLifetime
{
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"trips_{Guid.NewGuid():N}");

        await using TripsDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public TripsDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
}
