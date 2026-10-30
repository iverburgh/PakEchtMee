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

/// <summary>Applies the scope migration to a database that holds data from before families existed.</summary>
[IntegrationTest]
public sealed class AddFamilyScopeMigrationTests(SqlServerTestContainerFixture server)
{
    private static readonly Guid Verburgh = new(AddFamilyScope.Verburgh);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void The_migration_lists_exactly_the_current_seed_categories()
        => AddFamilyScope.SeedCategoryIds.Select(Guid.Parse).Order()
            .ShouldBe(CatalogSeedData.Categories.Select(category => category.Id).Order());

    [Fact]
    public async Task Seeded_categories_become_general_and_others_move_to_verburgh()
    {
        string connectionString = await server.CreateDatabaseAsync($"catalog_migration_{Guid.NewGuid():N}");
        await using CatalogDbContext context = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260918144920_InitialCatalog", cancellationToken: CancellationToken);

        Guid seeded = CatalogSeedData.CategoryId(CatalogSeedData.Tent);
        Guid userMade = Guid.CreateVersion7();
        await InsertCategoryAsync(connectionString, seeded, "Tent");
        await InsertCategoryAsync(connectionString, userMade, "Elektronica");

        await migrator.MigrateAsync(cancellationToken: CancellationToken);

        (await FamilyOfAsync(connectionString, seeded)).ShouldBeNull();
        (await FamilyOfAsync(connectionString, userMade)).ShouldBe(Verburgh);
    }

    private static async Task InsertCategoryAsync(string connectionString, Guid id, string name)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new("INSERT INTO [catalog].[Categories] ([Id], [Name]) VALUES (@id, @name)", connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", name);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<Guid?> FamilyOfAsync(string connectionString, Guid id)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken);
        await using SqlCommand command = new("SELECT [FamilyId] FROM [catalog].[Categories] WHERE [Id] = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        return await command.ExecuteScalarAsync(CancellationToken) as Guid?;
    }
}
