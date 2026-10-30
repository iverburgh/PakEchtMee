using Catalog.Domain.Categories;
using Catalog.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests;

[IntegrationTest]
public sealed class FamilyScopeTests(CatalogDatabaseFixture fixture) : IClassFixture<CatalogDatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static CategoryName AName(string value) => CategoryName.Create(value);

    private async Task<Category> GivenAsync(Category category)
    {
        await using CatalogDbContext context = fixture.CreateContext();
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken);

        return category;
    }

    [Fact]
    public async Task The_same_name_exists_in_two_families_and_in_general()
    {
        string name = $"Toiletries {Guid.NewGuid():N}";

        await GivenAsync(Category.CreateForFamily(Guid.CreateVersion7(), AName(name)));
        await GivenAsync(Category.CreateForFamily(Guid.CreateVersion7(), AName(name)));
        await GivenAsync(Category.Create(AName(name)));

        await using CatalogDbContext context = fixture.CreateContext();
        (await context.Categories.CountAsync(category => category.Name == AName(name), CancellationToken)).ShouldBe(3);
    }

    [Fact]
    public async Task The_database_rejects_a_duplicate_within_one_family()
    {
        Guid family = Guid.CreateVersion7();
        string name = $"Toiletries {Guid.NewGuid():N}";
        await GivenAsync(Category.CreateForFamily(family, AName(name)));

        await Should.ThrowAsync<DbUpdateException>(() => GivenAsync(Category.CreateForFamily(family, AName(name.ToUpperInvariant()))));
    }

    [Fact]
    public async Task The_database_rejects_a_duplicate_general_name()
    {
        string name = $"Tent {Guid.NewGuid():N}";
        await GivenAsync(Category.Create(AName(name)));

        await Should.ThrowAsync<DbUpdateException>(() => GivenAsync(Category.Create(AName(name))));
    }

    [Fact]
    public async Task Duplicate_detection_is_per_scope()
    {
        Guid family = Guid.CreateVersion7();
        string name = $"Toiletries {Guid.NewGuid():N}";
        await GivenAsync(Category.CreateForFamily(family, AName(name)));

        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);

        (await repository.ExistsWithNameAsync(AName(name), family, null, CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithNameAsync(AName(name), Guid.CreateVersion7(), null, CancellationToken)).ShouldBeFalse();
        (await repository.ExistsWithNameAsync(AName(name), null, null, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_family_sees_its_own_and_the_general_categories_but_not_another_familys()
    {
        Guid verburgh = Guid.CreateVersion7();
        Category own = await GivenAsync(Category.CreateForFamily(verburgh, AName($"Eigen {Guid.NewGuid():N}")));
        Category general = await GivenAsync(Category.Create(AName($"Algemeen {Guid.NewGuid():N}")));
        Category foreign = await GivenAsync(Category.CreateForFamily(Guid.CreateVersion7(), AName($"Ander {Guid.NewGuid():N}")));

        await using CatalogDbContext context = fixture.CreateContext();
        IReadOnlyList<Category> visible = await new CategoryRepository(context).ListVisibleAsync(verburgh, false, CancellationToken);

        visible.ShouldContain(category => category.Id == own.Id);
        visible.ShouldContain(category => category.Id == general.Id);
        visible.ShouldNotContain(category => category.Id == foreign.Id);
    }

    [Fact]
    public async Task Switching_a_general_category_off_and_on_is_persisted_per_family()
    {
        Guid verburgh = Guid.CreateVersion7();
        Guid jansen = Guid.CreateVersion7();
        Category general = await GivenAsync(Category.Create(AName($"Kampeerkeuken {Guid.NewGuid():N}")));

        await SwitchAsync(jansen, general.Id, on: false);

        (await SwitchedOffAsync(jansen)).ShouldContain(general.Id);
        (await SwitchedOffAsync(verburgh)).ShouldNotContain(general.Id);

        await SwitchAsync(jansen, general.Id, on: false);
        await SwitchAsync(jansen, general.Id, on: true);

        (await SwitchedOffAsync(jansen)).ShouldNotContain(general.Id);
    }

    private async Task SwitchAsync(Guid familyId, Guid categoryId, bool on)
    {
        await using CatalogDbContext context = fixture.CreateContext();
        CategoryRepository repository = new(context);
        Category category = (await repository.FindAsync(categoryId, CancellationToken))!;
        await repository.SwitchAsync(familyId, category, on, CancellationToken);
        await repository.SaveChangesAsync(CancellationToken);
    }

    private async Task<IReadOnlySet<Guid>> SwitchedOffAsync(Guid familyId)
    {
        await using CatalogDbContext context = fixture.CreateContext();

        return await new CategoryRepository(context).ListSwitchedOffAsync(familyId, CancellationToken);
    }
}
