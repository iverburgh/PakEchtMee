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

/// <summary>Applies the quantity-basis migration to a database that holds seeded items from before bases existed.</summary>
[IntegrationTest]
public sealed class AddQuantityBasisMigrationTests(SqlServerTestContainerFixture server)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Guid SeedItem(string category, string name) => CatalogSeedData.ItemId(category, name);

    [Theory]
    [InlineData(QuantityBasis.PerChild)]
    [InlineData(QuantityBasis.PerAdult)]
    [InlineData(QuantityBasis.PerPerson)]
    public void The_migration_lists_exactly_the_seeded_items_per_basis(QuantityBasis basis)
    {
        Guid[] listed = basis switch
        {
            QuantityBasis.PerChild => [.. AddQuantityBasis.SeededChildItems, .. AddQuantityBasis.SeededThreePerChildItems],
            QuantityBasis.PerAdult => AddQuantityBasis.SeededAdultItems,
            _ => AddQuantityBasis.SeededPersonItems,
        };

        listed.Order().ShouldBe(CatalogSeedData.PerHead
            .Where(entry => entry.Value == basis)
            .Select(entry => SeedItem(entry.Key.Category, entry.Key.Item))
            .Order());
    }

    [Fact]
    public async Task Untouched_seeded_items_get_their_rule_and_changed_ones_are_kept()
    {
        string connectionString = await server.CreateDatabaseAsync($"catalog_basis_{Guid.NewGuid():N}");
        await using CatalogDbContext context = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261030132315_AddFamilyScope", cancellationToken: CancellationToken);

        Guid slapen = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.KinderenSlapen);
        Guid buitenkleding = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.KinderenBuitenkleding);
        Guid kampeerkeuken = await InsertGeneralCategoryAsync(connectionString, CatalogSeedData.Kampeerkeuken);
        Guid sleepingBag = await InsertItemAsync(connectionString, slapen, CatalogSeedData.KinderenSlapen, "Slaapzak", 3, null);
        Guid jackets = await InsertItemAsync(connectionString, buitenkleding, CatalogSeedData.KinderenBuitenkleding, "Jassen", 4, null);
        Guid boots = await InsertItemAsync(connectionString, buitenkleding, CatalogSeedData.KinderenBuitenkleding, "Laarzen", null, null);
        Guid plates = await InsertItemAsync(connectionString, kampeerkeuken, CatalogSeedData.Kampeerkeuken, "Borden", 1, "stapel");
        Guid pans = await InsertItemAsync(connectionString, kampeerkeuken, CatalogSeedData.Kampeerkeuken, "Pannen", 2, null);

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        (await AmountOfAsync(connectionString, sleepingBag)).ShouldBe((1, "PerChild"));
        (await AmountOfAsync(connectionString, boots)).ShouldBe((1, "PerChild"));
        (await AmountOfAsync(connectionString, jackets)).ShouldBe((4, "Fixed"));
        (await AmountOfAsync(connectionString, plates)).ShouldBe((1, "Fixed"));
        (await AmountOfAsync(connectionString, pans)).ShouldBe((2, "Fixed"));
    }

    private static async Task<Guid> InsertGeneralCategoryAsync(string connectionString, string name)
    {
        Guid id = CatalogSeedData.CategoryId(name);
        await ExecuteAsync(
            connectionString,
            "INSERT INTO [catalog].[Categories] ([Id], [Name]) VALUES (@id, @name)",
            ("@id", id),
            ("@name", name));

        return id;
    }

    private static async Task<Guid> InsertItemAsync(string connectionString, Guid categoryId, string category, string name, int? quantity, string? unit)
    {
        Guid id = SeedItem(category, name);
        await ExecuteAsync(
            connectionString,
            "INSERT INTO [catalog].[Items] ([Id], [CategoryId], [Name], [Quantity], [Unit]) VALUES (@id, @category, @name, @quantity, @unit)",
            ("@id", id),
            ("@category", categoryId),
            ("@name", name),
            ("@quantity", (object?)quantity ?? DBNull.Value),
            ("@unit", (object?)unit ?? DBNull.Value));

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

    private static async Task<(int? Quantity, string? Basis)> AmountOfAsync(string connectionString, Guid id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new("SELECT [Quantity], [QuantityBasis] FROM [catalog].[Items] WHERE [Id] = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);

        return (reader.IsDBNull(0) ? null : reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }
}
