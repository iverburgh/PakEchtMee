using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Tests;
using Shouldly;
using Trips.Infrastructure.Sql.Migrations;
using Xunit;

namespace Trips.Infrastructure.Sql.Tests;

/// <summary>Applies the family migration to a database that holds a trip from before families existed.</summary>
[IntegrationTest]
public sealed class AddFamilyToTripsMigrationTests(SqlServerTestContainerFixture server)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_existing_trip_moves_to_verburgh()
    {
        string connectionString = await server.CreateDatabaseAsync($"trips_migration_{Guid.NewGuid():N}");
        await using TripsDbContext context = new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260918144912_InitialTrips", cancellationToken: CancellationToken);

        Guid tripId = Guid.CreateVersion7();
        await ExecuteAsync(connectionString, "INSERT INTO [trips].[Trips] ([Id], [Name]) VALUES (@id, 'Herfstvakantie')", tripId);

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new("SELECT [FamilyId] FROM [trips].[Trips] WHERE [Id] = @id", connection);
        command.Parameters.AddWithValue("@id", tripId);
        (await command.ExecuteScalarAsync(CancellationToken)).ShouldBe(new Guid(AddFamilyToTrips.Verburgh));
    }

    private static async Task ExecuteAsync(string connectionString, string sql, Guid id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }
}
