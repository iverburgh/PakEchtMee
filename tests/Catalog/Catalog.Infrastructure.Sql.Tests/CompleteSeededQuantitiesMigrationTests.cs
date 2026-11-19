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

/// <summary>Applies the migration that fills in the remaining seeded items to a database seeded before it existed.</summary>
[IntegrationTest]
public sealed class CompleteSeededQuantitiesMigrationTests(SqlServerTestContainerFixture server)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void The_migration_rows_equal_the_seed_rules()
    {
        IEnumerable<CompleteSeededQuantities.Row> expected = CatalogSeedData.SeedRules.Select(entry => new CompleteSeededQuantities.Row(
            CatalogSeedData.ItemId(entry.Key.Category, entry.Key.Item),
            entry.Value.Quantity,
            entry.Value.Unit!,
            entry.Value.Basis.ToString(),
            entry.Value.Period.ToString()));

        CompleteSeededQuantities.Rows.OrderBy(row => row.Id).ShouldBe(expected.OrderBy(row => row.Id));
    }

    [Fact]
    public async Task Untouched_items_get_their_rule_and_changed_ones_are_kept()
    {
        string connectionString = await server.CreateDatabaseAsync($"catalog_complete_{Guid.NewGuid():N}");
        await using CatalogDbContext context = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261119195114_AddQuantityPeriod", cancellationToken: CancellationToken);

        Guid tent = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.Tent);
        Guid kitchen = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.Kampeerkeuken);
        Guid camping = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.KamperenAlgemeen);
        Guid pegs = await InsertItemAsync(connectionString, tent, CatalogSeedData.Tent, "Haringen", null);
        Guid pans = await InsertItemAsync(connectionString, kitchen, CatalogSeedData.Kampeerkeuken, "Pannen", 2);
        Guid toiletPaper = await InsertItemAsync(connectionString, camping, CatalogSeedData.KamperenAlgemeen, "WC-papier", null);

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        (await RuleOfAsync(connectionString, pegs)).ShouldBe((30, null, "Fixed", "PerTrip"));
        (await RuleOfAsync(connectionString, toiletPaper)).ShouldBe((1, "rol", "Fixed", "PerDay"));
        (await RuleOfAsync(connectionString, pans)).ShouldBe((2, null, "Fixed", "PerTrip"));
    }

    private static async Task<Guid> InsertGeneralCategoryAsync(string connectionString, string name)
    {
        Guid id = CatalogSeedData.CategoryId(name);
        await ExecuteAsync(connectionString, "INSERT INTO [catalog].[Categories] ([Id], [Name]) VALUES (@id, @name)", ("@id", id), ("@name", name));

        return id;
    }

    private static async Task<Guid> InsertItemAsync(string connectionString, Guid categoryId, string category, string name, int? quantity)
    {
        Guid id = CatalogSeedData.ItemId(category, name);
        await ExecuteAsync(
            connectionString,
            """
            INSERT INTO [catalog].[Items] ([Id], [CategoryId], [Name], [Quantity], [QuantityBasis], [QuantityPeriod])
            VALUES (@id, @category, @name, @quantity, @basis, @period)
            """,
            ("@id", id),
            ("@category", categoryId),
            ("@name", name),
            ("@quantity", (object?)quantity ?? DBNull.Value),
            ("@basis", quantity is null ? DBNull.Value : nameof(QuantityBasis.Fixed)),
            ("@period", quantity is null ? DBNull.Value : nameof(QuantityPeriod.PerTrip)));

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

    private static async Task<(int? Quantity, string? Unit, string? Basis, string? Period)> RuleOfAsync(string connectionString, Guid id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new(
            "SELECT [Quantity], [Unit], [QuantityBasis], [QuantityPeriod] FROM [catalog].[Items] WHERE [Id] = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);

        return (
            reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }
}
