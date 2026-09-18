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
    }

    public Mock<ICategoryRepository> Categories { get; } = new();

    public Mock<ICatalogItemRepository> Items { get; } = new();

    public ServiceProvider Services { get; }

    public IMediator Mediator { get; }

    public Category GivenCategory(string name = "Toiletries")
    {
        Category category = Category.Create(CategoryName.Create(name));
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
