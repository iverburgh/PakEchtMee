using Catalog.Domain.Categories;
using Catalog.Infrastructure.Sql.Repositories;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests;

[IntegrationTest]
public sealed class CategoryRepositoryTests(CatalogDatabaseFixture fixture) : IClassFixture<CatalogDatabaseFixture>
{
    private static CategoryName AName(string value) => CategoryName.Create(value);

    private async Task<Category> GivenCategoryAsync(string name)
    {
        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);
        Category category = Category.Create(AName(name));
        repository.Add(category);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category;
    }

    [Fact]
    public async Task A_stored_category_can_be_read_back()
    {
        Category created = await GivenCategoryAsync($"Toiletries {Guid.NewGuid():N}");

        await using CatalogDbContext context = fixture.CreateContext();
        Category? found = await new CategoryRepository(context).FindAsync(created.Id, TestContext.Current.CancellationToken);

        found.ShouldNotBeNull();
        found.Name.Value.ShouldBe(created.Name.Value);
        found.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task An_unknown_category_returns_nothing()
    {
        await using CatalogDbContext context = fixture.CreateContext();

        Category? found = await new CategoryRepository(context).FindAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task Renaming_is_persisted()
    {
        Category created = await GivenCategoryAsync($"Bathroom {Guid.NewGuid():N}");
        string newName = $"Washroom {Guid.NewGuid():N}";

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CategoryRepository repository = new(context);
            Category category = (await repository.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            category.Rename(AName(newName));
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using CatalogDbContext verification = fixture.CreateContext();
        Category? reloaded = await new CategoryRepository(verification).FindAsync(created.Id, TestContext.Current.CancellationToken);

        reloaded!.Name.Value.ShouldBe(newName);
    }

    [Fact]
    public async Task A_removed_category_disappears_from_the_default_list_and_comes_back_on_restore()
    {
        Category created = await GivenCategoryAsync($"Ski gear {Guid.NewGuid():N}");

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CategoryRepository repository = new(context);
            Category category = (await repository.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            category.Delete(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CategoryRepository repository = new(context);
            (await repository.ListAsync(false, TestContext.Current.CancellationToken)).ShouldNotContain(category => category.Id == created.Id);
            (await repository.ListAsync(true, TestContext.Current.CancellationToken)).ShouldContain(category => category.Id == created.Id);
        }

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CategoryRepository repository = new(context);
            Category category = (await repository.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            category.Restore();
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using CatalogDbContext verification = fixture.CreateContext();
        (await new CategoryRepository(verification).ListAsync(false, TestContext.Current.CancellationToken))
            .ShouldContain(category => category.Id == created.Id);
    }

    [Fact]
    public async Task Duplicate_detection_ignores_case_and_accents()
    {
        string name = $"Kaarsen{Guid.NewGuid():N}";
        await GivenCategoryAsync(name);

        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);

        (await repository.ExistsWithNameAsync(AName(name.ToUpperInvariant()), null, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithNameAsync(AName(name.Replace("Kaarsen", "Käarsen", StringComparison.Ordinal)), null, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Duplicate_detection_skips_the_category_being_renamed()
    {
        Category created = await GivenCategoryAsync($"Electronics {Guid.NewGuid():N}");

        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);

        (await repository.ExistsWithNameAsync(created.Name, created.Id, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Duplicate_detection_ignores_removed_categories()
    {
        Category created = await GivenCategoryAsync($"Camping {Guid.NewGuid():N}");

        await using (CatalogDbContext context = fixture.CreateContext())
        {
            CategoryRepository repository = new(context);
            Category category = (await repository.FindAsync(created.Id, TestContext.Current.CancellationToken))!;
            category.Delete(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using CatalogDbContext verification = fixture.CreateContext();

        (await new CategoryRepository(verification).ExistsWithNameAsync(created.Name, null, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
