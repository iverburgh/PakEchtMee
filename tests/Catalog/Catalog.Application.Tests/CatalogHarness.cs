using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Catalog.Application.Tests;

/// <summary>Runs handlers through the real MediatR pipeline so the behaviours are exercised together with the handler.</summary>
internal sealed class CatalogHarness
{
    public CatalogHarness()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(Categories.Object);
        services.AddSingleton(Items.Object);
        services.AddCatalogApplication();

        Services = services.BuildServiceProvider();
        Mediator = Services.GetRequiredService<IMediator>();

        // Nothing is switched off unless a test says so, and lookups by several ids see every given category.
        Categories
            .Setup(repository => repository.ListSwitchedOffAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => SwitchedOff);
        Categories
            .Setup(repository => repository.FindManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => [.. given.Where(category => ids.Contains(category.Id))]);
    }

    private readonly List<Category> given = [];

    /// <summary>General categories the default family switched off.</summary>
    public HashSet<Guid> SwitchedOff { get; } = [];

    /// <summary>The family the default actor belongs to; <see cref="GivenCategory"/> creates its own categories.</summary>
    public static readonly Guid Family = Guid.CreateVersion7();

    public static CatalogActor Member => CatalogActor.MemberOf(Family);

    public Mock<ICategoryRepository> Categories { get; } = new();

    public Mock<ICatalogItemRepository> Items { get; } = new();

    public ServiceProvider Services { get; }

    public IMediator Mediator { get; }

    public Category GivenCategory(string name = "Toiletries") => Given(Category.CreateForFamily(Family, CategoryName.Create(name)));

    public Category GivenGeneralCategory(string name = "Tent") => Given(Category.Create(CategoryName.Create(name)));

    public Category GivenCategoryOfAnotherFamily(string name = "Eigen spullen") => Given(Category.CreateForFamily(Guid.CreateVersion7(), CategoryName.Create(name)));

    private Category Given(Category category)
    {
        given.Add(category);
        Categories.Setup(repository => repository.FindAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        return category;
    }

    public CatalogItem GivenItem(Category category, string name = "Toothbrush", int? quantity = null, string? unit = null)
    {
        CatalogItem item = CatalogItem.Create(category, ItemName.Create(name), Amount.Create(quantity, unit));
        Items.Setup(repository => repository.FindAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        return item;
    }
}
