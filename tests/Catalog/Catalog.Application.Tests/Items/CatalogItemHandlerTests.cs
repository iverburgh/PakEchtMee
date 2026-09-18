using Catalog.Application.Contracts;
using Catalog.Application.Items;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Application.Tests.Items;

public sealed class CatalogItemHandlerTests
{
    private readonly CatalogHarness harness = new();

    [Fact]
    public async Task Creating_an_item_stores_it_in_the_category()
    {
        Category category = harness.GivenCategory();

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(category.Id, "Socks", 7, "pair"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Socks");
        result.Value.Quantity.ShouldBe(7);
        result.Value.Unit.ShouldBe("pair");
        result.Value.CategoryId.ShouldBe(category.Id);
        harness.Items.Verify(repository => repository.Add(It.IsAny<CatalogItem>()), Times.Once);
        harness.Items.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Creating_an_item_in_an_unknown_category_reports_not_found()
    {
        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(Guid.CreateVersion7(), "Socks", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Creating_an_item_in_a_removed_category_is_rejected()
    {
        Category category = harness.GivenCategory();
        category.Delete(DateTimeOffset.UtcNow);

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(category.Id, "Socks", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Creating_a_duplicate_item_name_in_the_same_category_is_rejected()
    {
        Category category = harness.GivenCategory();
        harness.Items
            .Setup(repository => repository.ExistsWithNameInCategoryAsync(category.Id, It.IsAny<ItemName>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(category.Id, "toothbrush", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Items.Verify(repository => repository.Add(It.IsAny<CatalogItem>()), Times.Never);
    }

    [Fact]
    public async Task Creating_an_item_with_a_unit_but_no_quantity_is_rejected()
    {
        Category category = harness.GivenCategory();

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(category.Id, "Socks", null, "pair"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failed_result()
    {
        Category category = harness.GivenCategory();
        harness.Items
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new CreateCatalogItemHandler.Command(category.Id, "Socks", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Updating_an_item_replaces_the_name_and_the_amount()
    {
        Category category = harness.GivenCategory();
        CatalogItem item = harness.GivenItem(category, "Towel");

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new UpdateCatalogItemHandler.Command(item.Id, "Beach towel", 2, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Beach towel");
        result.Value.Quantity.ShouldBe(2);
        result.Value.Unit.ShouldBeNull();
    }

    [Fact]
    public async Task Updating_an_unknown_item_reports_not_found()
    {
        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new UpdateCatalogItemHandler.Command(Guid.CreateVersion7(), "Towel", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Updating_to_a_name_that_already_exists_is_rejected()
    {
        Category category = harness.GivenCategory();
        CatalogItem item = harness.GivenItem(category, "Towel");
        harness.Items
            .Setup(repository => repository.ExistsWithNameInCategoryAsync(category.Id, It.IsAny<ItemName>(), item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new UpdateCatalogItemHandler.Command(item.Id, "Toothbrush", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        item.Name.Value.ShouldBe("Towel");
    }

    [Fact]
    public async Task Moving_an_item_changes_its_category()
    {
        Category source = harness.GivenCategory("Electronics");
        Category target = harness.GivenCategory("Travel documents");
        CatalogItem item = harness.GivenItem(source, "Charger");

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new MoveCatalogItemHandler.Command(item.Id, target.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(target.Id);
    }

    [Fact]
    public async Task Moving_an_item_to_an_unknown_category_reports_not_found()
    {
        Category source = harness.GivenCategory("Electronics");
        CatalogItem item = harness.GivenItem(source, "Charger");

        Result<CatalogItemDto, Exception> result = await harness.Mediator.Send(
            new MoveCatalogItemHandler.Command(item.Id, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Removing_and_restoring_an_item()
    {
        Category category = harness.GivenCategory();
        CatalogItem item = harness.GivenItem(category);

        Result<CatalogItemDto, Exception> removed = await harness.Mediator.Send(
            new RemoveCatalogItemHandler.Command(item.Id), TestContext.Current.CancellationToken);
        removed.IsSuccess.ShouldBeTrue();
        removed.Value.IsDeleted.ShouldBeTrue();

        Result<CatalogItemDto, Exception> restored = await harness.Mediator.Send(
            new RestoreCatalogItemHandler.Command(item.Id), TestContext.Current.CancellationToken);
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Listing_the_items_of_a_category_maps_them()
    {
        Category category = harness.GivenCategory();
        harness.Items
            .Setup(repository => repository.ListByCategoryAsync(category.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CatalogItem.Create(category, ItemName.Create("Toothbrush"), null)]);

        Result<IReadOnlyList<CatalogItemDto>, Exception> result = await harness.Mediator.Send(
            new ListCatalogItemsHandler.Query(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Single().Name.ShouldBe("Toothbrush");
    }

    [Fact]
    public async Task Listing_items_for_no_categories_returns_nothing_without_hitting_the_repository()
    {
        Result<IReadOnlyList<CatalogItemDto>, Exception> result = await harness.Mediator.Send(
            new ListCatalogItemsForCategoriesHandler.Query([]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
        harness.Items.Verify(
            repository => repository.ListByCategoriesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Listing_items_for_several_categories_maps_them()
    {
        Category first = harness.GivenCategory("Toiletries");
        Category second = harness.GivenCategory("Electronics");
        harness.Items
            .Setup(repository => repository.ListByCategoriesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CatalogItem.Create(first, ItemName.Create("Toothbrush"), null),
                CatalogItem.Create(second, ItemName.Create("Charger"), Amount.Create(2, null)),
            ]);

        Result<IReadOnlyList<CatalogItemDto>, Exception> result = await harness.Mediator.Send(
            new ListCatalogItemsForCategoriesHandler.Query([first.Id, second.Id]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[1].Quantity.ShouldBe(2);
    }
}
