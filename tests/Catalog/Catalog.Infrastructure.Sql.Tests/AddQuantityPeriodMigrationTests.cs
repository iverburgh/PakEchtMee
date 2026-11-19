using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Migrations;
using Catalog.Infrastructure.Sql.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests;

/// <summary>Applies the quantity-period migration to a database that holds seeded clothing from before periods existed.</summary>
[IntegrationTest]
public sealed class AddQuantityPeriodMigrationTests(SqlServerTestContainerFixture server)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(QuantityBasis.PerAdult)]
    [InlineData(QuantityBasis.PerChild)]
    public void The_migration_lists_exactly_the_seeded_per_day_items(QuantityBasis basis)
    {
        Guid[] listed = basis is QuantityBasis.PerAdult ? AddQuantityPeriod.SeededAdultPerDayItems : AddQuantityPeriod.SeededChildPerDayItems;

        listed.Order().ShouldBe(CatalogSeedData.PerHead
            .Where(entry => entry.Value == new SeedRule(basis, QuantityPeriod.PerDay))
            .Select(entry => CatalogSeedData.ItemId(entry.Key.Category, entry.Key.Item))
            .Order());
    }

    [Fact]
    public async Task Untouched_clothing_becomes_per_day_and_everything_else_is_once_per_trip()
    {
        string connectionString = await server.CreateDatabaseAsync($"catalog_period_{Guid.NewGuid():N}");
        await using CatalogDbContext context = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261104192410_AddQuantityBasis", cancellationToken: CancellationToken);

        Guid kleding = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.Kleding);
        Guid kinderenKleding = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.KinderenKleding);
        Guid socks = await InsertItemAsync(connectionString, kleding, CatalogSeedData.Kleding, "Sokken", null, null);
        Guid underwear = await InsertItemAsync(connectionString, kleding, CatalogSeedData.Kleding, "Onderbroeken", 10, "Fixed");
        Guid childSocks = await InsertItemAsync(connectionString, kinderenKleding, CatalogSeedData.KinderenKleding, "Sokken", null, null);
        Guid shirts = await InsertItemAsync(connectionString, kleding, CatalogSeedData.Kleding, "T-shirts", 2, "PerPerson");

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        (await RuleOfAsync(connectionString, socks)).ShouldBe((1, "PerAdult", "PerDay"));
        (await RuleOfAsync(connectionString, childSocks)).ShouldBe((1, "PerChild", "PerDay"));
        (await RuleOfAsync(connectionString, underwear)).ShouldBe((10, "Fixed", "PerTrip"));
        (await RuleOfAsync(connectionString, shirts)).ShouldBe((2, "PerPerson", "PerTrip"));
    }

    private static async Task<Guid> InsertGeneralCategoryAsync(string connectionString, string name)
    {
        Guid id = CatalogSeedData.CategoryId(name);
        await ExecuteAsync(connectionString, "INSERT INTO [catalog].[Categories] ([Id], [Name]) VALUES (@id, @name)", ("@id", id), ("@name", name));

        return id;
    }

    private static async Task<Guid> InsertItemAsync(string connectionString, Guid categoryId, string category, string name, int? quantity, string? basis)
    {
        Guid id = CatalogSeedData.ItemId(category, name);
        await ExecuteAsync(
            connectionString,
            "INSERT INTO [catalog].[Items] ([Id], [CategoryId], [Name], [Quantity], [QuantityBasis]) VALUES (@id, @category, @name, @quantity, @basis)",
            ("@id", id),
            ("@category", categoryId),
            ("@name", name),
            ("@quantity", (object?)quantity ?? DBNull.Value),
            ("@basis", (object?)basis ?? DBNull.Value));

        return id;
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

    private static async Task<(int? Quantity, string? Basis, string? Period)> RuleOfAsync(string connectionString, Guid id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new("SELECT [Quantity], [QuantityBasis], [QuantityPeriod] FROM [catalog].[Items] WHERE [Id] = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);

        return (
            reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }
}
