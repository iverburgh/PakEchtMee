using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests.Seeding;

/// <summary>Every test gets its own database, because the seed ids are the same in every run.</summary>
[IntegrationTest]
public sealed class CatalogSeederTests(SqlServerTestContainerFixture server)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Func<CatalogDbContext>> NewDatabaseAsync()
    {
        string connectionString = await server.CreateDatabaseAsync($"catalog_seed_{Guid.NewGuid():N}");
        CatalogDbContext CreateContext() => new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);

        await using CatalogDbContext context = CreateContext();
        await context.Database.MigrateAsync(CancellationToken);

        return CreateContext;
    }

    private static async Task<CatalogSeedResult> SeedAsync(Func<CatalogDbContext> createContext)
    {
        await using CatalogDbContext context = createContext();

        return await new CatalogSeeder(context, NullLogger<CatalogSeeder>.Instance).SeedAsync(CatalogSeedData.Categories, CancellationToken);
    }

    private static async Task ChangeCategoryAsync(Func<CatalogDbContext> createContext, string seedName, Action<Category> change)
    {
        await using CatalogDbContext context = createContext();
        Category category = await context.Categories.SingleAsync(category => category.Id == CatalogSeedData.CategoryId(seedName), CancellationToken);
        change(category);
        await context.SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task An_empty_database_gets_the_whole_catalog()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ShouldBe(new CatalogSeedResult(22, 149));
        await using CatalogDbContext context = createContext();
        (await context.Categories.CountAsync(CancellationToken)).ShouldBe(22);
        (await context.Items.CountAsync(CancellationToken)).ShouldBe(149);
    }

    [Fact]
    public async Task Quantities_are_stored()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();

        await SeedAsync(createContext);

        await using CatalogDbContext context = createContext();
        CatalogItem sleepingBag = await context.Items.SingleAsync(
            item => item.Id == CatalogSeedData.ItemId(CatalogSeedData.KinderenSlapen, "Slaapzak"),
            CancellationToken);
        sleepingBag.Amount!.Quantity.ShouldBe(3);
        sleepingBag.Amount.Unit.ShouldBeNull();
    }

    [Fact]
    public async Task A_second_run_inserts_nothing()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ShouldBe(new CatalogSeedResult(0, 0));
        await using CatalogDbContext context = createContext();
        (await context.Items.CountAsync(CancellationToken)).ShouldBe(149);
    }

    [Fact]
    public async Task A_removed_seed_category_stays_removed()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);
        await ChangeCategoryAsync(createContext, CatalogSeedData.Koken, category => category.Delete(Now));

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ShouldBe(new CatalogSeedResult(0, 0));
        await using CatalogDbContext context = createContext();
        Category koken = await context.Categories.SingleAsync(category => category.Id == CatalogSeedData.CategoryId(CatalogSeedData.Koken), CancellationToken);
        koken.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task A_removed_seed_category_gets_no_new_items()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);
        Guid kruidenId = CatalogSeedData.ItemId(CatalogSeedData.Koken, "Kruiden");
        await using (CatalogDbContext context = createContext())
        {
            await context.Items.Where(item => item.Id == kruidenId).ExecuteDeleteAsync(CancellationToken);
        }

        await ChangeCategoryAsync(createContext, CatalogSeedData.Koken, category => category.Delete(Now));

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ItemsAdded.ShouldBe(0);
        await using CatalogDbContext check = createContext();
        (await check.Items.AnyAsync(item => item.Id == kruidenId, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_renamed_seed_category_keeps_its_new_name()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);
        await ChangeCategoryAsync(createContext, CatalogSeedData.Koken, category => category.Rename(CategoryName.Create("Kookspullen")));

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ShouldBe(new CatalogSeedResult(0, 0));
        await using CatalogDbContext context = createContext();
        Category koken = await context.Categories.SingleAsync(category => category.Id == CatalogSeedData.CategoryId(CatalogSeedData.Koken), CancellationToken);
        koken.Name.Value.ShouldBe("Kookspullen");
        (await context.Categories.CountAsync(CancellationToken)).ShouldBe(22);
    }

    [Fact]
    public async Task A_user_category_with_a_seed_name_is_left_alone()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        Category userCategory = Category.Create(CategoryName.Create("keukengerei"));
        await using (CatalogDbContext context = createContext())
        {
            context.Categories.Add(userCategory);
            await context.SaveChangesAsync(CancellationToken);
        }

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ShouldBe(new CatalogSeedResult(21, 145));
        await using CatalogDbContext check = createContext();
        Category kept = await check.Categories.SingleAsync(category => category.Id == userCategory.Id, CancellationToken);
        kept.Name.Value.ShouldBe("keukengerei");
        (await check.Categories.AnyAsync(category => category.Id == CatalogSeedData.CategoryId(CatalogSeedData.Keukengerei), CancellationToken))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task A_user_item_with_a_seed_name_is_left_alone()
    {
        Func<CatalogDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);
        Guid kruidenId = CatalogSeedData.ItemId(CatalogSeedData.Koken, "Kruiden");
        await using (CatalogDbContext context = createContext())
        {
            await context.Items.Where(item => item.Id == kruidenId).ExecuteDeleteAsync(CancellationToken);
            Category koken = await context.Categories.SingleAsync(category => category.Id == CatalogSeedData.CategoryId(CatalogSeedData.Koken), CancellationToken);
            context.Items.Add(CatalogItem.Create(koken, ItemName.Create("KRUIDEN"), null));
            await context.SaveChangesAsync(CancellationToken);
        }

        CatalogSeedResult result = await SeedAsync(createContext);

        result.ItemsAdded.ShouldBe(0);
        await using CatalogDbContext check = createContext();
        (await check.Items.AnyAsync(item => item.Id == kruidenId, CancellationToken)).ShouldBeFalse();
    }
}
