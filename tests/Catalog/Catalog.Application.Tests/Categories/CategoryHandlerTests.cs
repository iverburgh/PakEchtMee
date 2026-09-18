using Catalog.Application.Categories;
using Catalog.Application.Contracts;
using Catalog.Domain.Categories;
using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Application.Tests.Categories;

public sealed class CategoryHandlerTests
{
    private readonly CatalogHarness harness = new();

    [Fact]
    public async Task Creating_a_category_stores_and_returns_it()
    {
        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new CreateCategoryHandler.Command("  Toiletries  "), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Toiletries");
        harness.Categories.Verify(repository => repository.Add(It.Is<Category>(category => category.Name.Value == "Toiletries")), Times.Once);
        harness.Categories.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Creating_a_category_with_an_existing_name_is_rejected()
    {
        harness.Categories
            .Setup(repository => repository.ExistsWithNameAsync(It.IsAny<CategoryName>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new CreateCategoryHandler.Command("toiletries"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Categories.Verify(repository => repository.Add(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Creating_a_category_without_a_name_is_rejected()
    {
        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new CreateCategoryHandler.Command("   "), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failed_result()
    {
        harness.Categories
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new CreateCategoryHandler.Command("Toiletries"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Renaming_an_unknown_category_reports_not_found()
    {
        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RenameCategoryHandler.Command(Guid.CreateVersion7(), "Bathroom"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Renaming_a_category_returns_the_new_name()
    {
        Category category = harness.GivenCategory();

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RenameCategoryHandler.Command(category.Id, "Bathroom"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Bathroom");
        harness.Categories.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Renaming_a_category_to_an_existing_name_is_rejected()
    {
        Category category = harness.GivenCategory();
        harness.Categories
            .Setup(repository => repository.ExistsWithNameAsync(It.IsAny<CategoryName>(), category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RenameCategoryHandler.Command(category.Id, "Bathroom"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        category.Name.Value.ShouldBe("Toiletries");
    }

    [Fact]
    public async Task Removing_a_category_marks_it_deleted()
    {
        Category category = harness.GivenCategory("Ski gear");

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RemoveCategoryHandler.Command(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsDeleted.ShouldBeTrue();
        category.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Removing_an_unknown_category_reports_not_found()
    {
        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RemoveCategoryHandler.Command(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Restoring_a_category_brings_it_back()
    {
        Category category = harness.GivenCategory("Ski gear");
        category.Delete(DateTimeOffset.UtcNow);

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RestoreCategoryHandler.Command(category.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Restoring_is_rejected_when_the_name_is_taken_again()
    {
        Category category = harness.GivenCategory("Ski gear");
        category.Delete(DateTimeOffset.UtcNow);
        harness.Categories
            .Setup(repository => repository.ExistsWithNameAsync(It.IsAny<CategoryName>(), category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<CategoryDto, Exception> result = await harness.Mediator.Send(
            new RestoreCategoryHandler.Command(category.Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        category.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Listing_maps_every_category()
    {
        harness.Categories
            .Setup(repository => repository.ListAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Category.Create(CategoryName.Create("Toiletries")), Category.Create(CategoryName.Create("Electronics"))]);

        Result<IReadOnlyList<CategoryDto>, Exception> result = await harness.Mediator.Send(
            new ListCategoriesHandler.Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(category => category.Name).ShouldBe(["Toiletries", "Electronics"]);
    }

    [Fact]
    public async Task Listing_fails_as_a_result_when_the_repository_throws()
    {
        harness.Categories
            .Setup(repository => repository.ListAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("database unavailable"));

        Result<IReadOnlyList<CategoryDto>, Exception> result = await harness.Mediator.Send(
            new ListCategoriesHandler.Query(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<TimeoutException>();
    }
}
