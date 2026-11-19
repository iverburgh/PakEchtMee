using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Tests;
using Shouldly;
using Trips.Domain;
using Trips.Infrastructure.Sql.Repositories;
using Xunit;

namespace Trips.Infrastructure.Sql.Tests;

/// <summary>Applies the period migration to a database that holds trip items from before periods existed.</summary>
[IntegrationTest]
public sealed class AddQuantityPeriodMigrationTests(SqlServerTestContainerFixture server)
{
    private static readonly Guid Family = Guid.CreateVersion7();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Existing_items_count_once_per_trip_and_keep_their_total()
    {
        string connectionString = await server.CreateDatabaseAsync($"trips_period_{Guid.NewGuid():N}");
        await using TripsDbContext context = new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261104193001_AddQuantityPerPerson", cancellationToken: CancellationToken);

        Guid tripId = Guid.CreateVersion7();
        Guid jackets = Guid.CreateVersion7();
        await ExecuteAsync(
            connectionString,
            "INSERT INTO [trips].[Trips] ([Id], [Name], [FamilyId], [Adults], [Children]) VALUES (@id, N'Kamperen', @family, 2, 3)",
            ("@id", tripId),
            ("@family", Family));
        await ExecuteAsync(
            connectionString,
            """
            INSERT INTO [trips].[TripItems] ([Id], [TripId], [SourceCategoryId], [SourceItemId], [CategoryName], [Name], [Quantity], [QuantityBasis], [Total], [Status])
            VALUES (@id, @trip, @category, @source, N'Kinderen – Buitenkleding', N'Jassen', 1, N'PerChild', 3, 0)
            """,
            ("@id", jackets),
            ("@trip", tripId),
            ("@category", Guid.CreateVersion7()),
            ("@source", Guid.CreateVersion7()));

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        await using TripsDbContext verify = new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
        Trip? trip = await new TripRepository(verify).FindAsync(tripId, Family, CancellationToken);

        trip.ShouldNotBeNull();
        TripItem item = trip.Items.Single(item => item.Id == jackets);
        item.Amount.ShouldBe(Amount.Create(1, null, QuantityBasis.PerChild, QuantityPeriod.PerTrip));
        item.Total.ShouldBe(3);
    }

    private static async Task ExecuteAsync(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new(sql, connection);

        foreach ((string name, object value) in parameters)
            command.Parameters.AddWithValue(name, value);

        await command.ExecuteNonQueryAsync(CancellationToken);
    }
}
