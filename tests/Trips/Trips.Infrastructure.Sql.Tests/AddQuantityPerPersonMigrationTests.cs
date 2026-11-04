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

/// <summary>Applies the quantity migration to a database that holds a trip from before compositions and bases existed.</summary>
[IntegrationTest]
public sealed class AddQuantityPerPersonMigrationTests(SqlServerTestContainerFixture server)
{
    private static readonly Guid Family = Guid.CreateVersion7();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_existing_trip_gets_the_default_composition_and_keeps_its_quantities()
    {
        string connectionString = await server.CreateDatabaseAsync($"trips_quantity_{Guid.NewGuid():N}");
        await using TripsDbContext context = new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261030133208_AddFamilyToTrips", cancellationToken: CancellationToken);

        Guid tripId = Guid.CreateVersion7();
        Guid sleepingBags = Guid.CreateVersion7();
        Guid umbrella = Guid.CreateVersion7();
        await ExecuteAsync(
            connectionString,
            "INSERT INTO [trips].[Trips] ([Id], [Name], [FamilyId]) VALUES (@id, N'Kamperen', @family)",
            ("@id", tripId),
            ("@family", Family));
        await InsertItemAsync(connectionString, tripId, sleepingBags, "Slaapzak", 3);
        await InsertItemAsync(connectionString, tripId, umbrella, "Paraplu", null);

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        await using TripsDbContext verify = new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);
        Trip? trip = await new TripRepository(verify).FindAsync(tripId, Family, CancellationToken);

        trip.ShouldNotBeNull();
        trip.Composition.ShouldBe(TripComposition.Create(2, 0));
        TripItem bags = trip.Items.Single(item => item.Id == sleepingBags);
        bags.Amount.ShouldBe(Amount.Create(3, null));
        bags.Total.ShouldBe(3);
        TripItem plain = trip.Items.Single(item => item.Id == umbrella);
        plain.Amount.ShouldBeNull();
        plain.Total.ShouldBeNull();
    }

    private static Task InsertItemAsync(string connectionString, Guid tripId, Guid id, string name, int? quantity)
        => ExecuteAsync(
            connectionString,
            """
            INSERT INTO [trips].[TripItems] ([Id], [TripId], [SourceCategoryId], [SourceItemId], [CategoryName], [Name], [Quantity], [Status])
            VALUES (@id, @trip, @category, @source, N'Kinderen – Slapen', @name, @quantity, 0)
            """,
            ("@id", id),
            ("@trip", tripId),
            ("@category", Guid.CreateVersion7()),
            ("@source", Guid.CreateVersion7()),
            ("@name", name),
            ("@quantity", (object?)quantity ?? DBNull.Value));

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
