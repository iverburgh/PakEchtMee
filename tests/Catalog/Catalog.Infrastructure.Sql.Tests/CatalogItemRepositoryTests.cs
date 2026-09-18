using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Repositories;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests;

[IntegrationTest]
public sealed class CatalogItemRepositoryTests(CatalogDatabaseFixture fixture) : IClassFixture<CatalogDatabaseFixture>
{
    private async Task<Category> GivenCategoryAsync()
    {
        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);
        Category category = Category.Create(CategoryName.Create($"Category {Guid.NewGuid():N}"));
        repository.Add(category);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category;
    }

    private async Task<CatalogItem> GivenItemAsync(Category category, string name, int? quantity = null, string? unit = null)
    {
        await using CatalogDbContext context = fixture.CreateContext();
        CatalogItemRepository repository = new(context);
        CatalogItem item = CatalogItem.Create(category, ItemName.Create(name), Amount.Create(quantity, unit));
        repository.Add(item);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        return item;
    }

    [Fact]
    public async Task An_item_keeps_its_amount_across_a_round_trip()
    {
        Category category = await GivenCategoryAsync();
        CatalogItem created = await GivenItemAsync(category, "Socks", 7, "pair");

        await using CatalogDbContext context = fixture.CreateContext();
        CatalogItem? found = await new CatalogItemRepository(context).FindAsync(created.Id, TestContext.Current.CancellationToken);

        found.ShouldNotBeNull();
        found.Amount!.Quantity.ShouldBe(7);
        found.Amount.Unit.ShouldBe("pair");
    }

    [Fact]
    public async Task An_item_without_an_amount_round_trips_as_null()
    {
        Category category = await GivenCategoryAsync();
        CatalogItem created = await GivenItemAsync(category, "Passport");

        await using CatalogDbContext context = fixture.CreateContext();
        CatalogItem? found = await new CatalogItemRepository(context).FindAsync(created.Id, TestContext.Current.CancellationToken);

        found!.Amount.ShouldBeNull();
    }

    [Fact]
    public async Task An_unknown_item_returns_nothing()
    {
        await using CatalogDbContext context = fixture.CreateContext();

        (await new CatalogItemRepository(context).FindAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Listing_a_category_without_items_is_empty()
    {
        Category category = await GivenCategoryAsync();

        await using CatalogDbContext context = fixture.CreateContext();

        (await new CatalogItemRepository(context).ListByCategoryAsync(category.Id, false, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Removed_items_are_hidden_unless_explicitly_requested()
    {
        Category category = await GivenCategoryAsync();
        CatalogItem created = await GivenItemAsync(category, "Ski poles");

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CatalogItemRepository repository = new(context);
            CatalogItem item = (await repository.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            item.Delete(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using CatalogDbContext verification = fixture.CreateContext();
        CatalogItemRepository verify = new(verification);

        (await verify.ListByCategoryAsync(category.Id, false, TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await verify.ListByCategoryAsync(category.Id, true, TestContext.Current.CancellationToken)).Count.ShouldBe(1);
        (await verify.ListByCategoriesAsync([category.Id], TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Listing_several_categories_returns_their_live_items()
    {
        Category first = await GivenCategoryAsync();
        Category second = await GivenCategoryAsync();
        await GivenItemAsync(first, "Toothbrush");
        await GivenItemAsync(second, "Charger");

        await using CatalogDbContext context = fixture.CreateContext();

        (await new CatalogItemRepository(context).ListByCategoriesAsync([first.Id, second.Id], TestContext.Current.CancellationToken))
            .Count.ShouldBe(2);
    }

    [Fact]
    public async Task Duplicate_detection_is_scoped_to_the_category_and_ignores_case_and_accents()
    {
        Category first = await GivenCategoryAsync();
        Category second = await GivenCategoryAsync();
        await GivenItemAsync(first, "Kaarsen");

        await using CatalogDbContext context = fixture.CreateContext();
        CatalogItemRepository repository = new(context);

        (await repository.ExistsWithNameInCategoryAsync(first.Id, ItemName.Create("KAARSEN"), null, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithNameInCategoryAsync(first.Id, ItemName.Create("Käarsen"), null, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithNameInCategoryAsync(second.Id, ItemName.Create("Kaarsen"), null, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Moving_an_item_is_persisted()
    {
        Category source = await GivenCategoryAsync();
        Category target = await GivenCategoryAsync();
        CatalogItem created = await GivenItemAsync(source, "Charger");

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CatalogItemRepository items = new(context);
            CategoryRepository categories = new(context);
            CatalogItem item = (await items.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            Category destination = (await categories.FindAsync(target.Id, TestContext.Current.CancellationToken))!;
            item.MoveTo(destination);
            await items.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using CatalogDbContext verification = fixture.CreateContext();
        CatalogItem? reloaded = await new CatalogItemRepository(verification).FindAsync(created.Id, TestContext.Current.CancellationToken);

        reloaded!.CategoryId.ShouldBe(target.Id);
    }
}
