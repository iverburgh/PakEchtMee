using Catalog.Application.Categories;
using Catalog.Application.Contracts;
using Catalog.Application.Items;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Application.Tests;

/// <summary>The family and general-catalog rules, checked through every handler that reads or changes the catalog.</summary>
public sealed class CatalogAccessTests
{
    private readonly CatalogHarness harness = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static CatalogActor Member => CatalogHarness.Member;

    private Task<Result<T, Exception>> SendAsync<T>(IRequest<Result<T, Exception>> request) => harness.Mediator.Send(request, CancellationToken);

    public static TheoryData<string> CategoryWrites => ["rename", "remove", "restore"];

    private static IRequest<Result<CategoryDto, Exception>> CategoryWrite(string kind, CatalogActor actor, Guid id) => kind switch
    {
        "rename" => new RenameCategoryHandler.Command(actor, id, "Nieuwe naam"),
        "remove" => new RemoveCategoryHandler.Command(actor, id),
        _ => new RestoreCategoryHandler.Command(actor, id),
    };

    public static TheoryData<string> ItemWrites => ["update", "remove", "restore", "move"];

    private static IRequest<Result<CatalogItemDto, Exception>> ItemWrite(string kind, CatalogActor actor, Guid id, Guid target) => kind switch
    {
        "update" => new UpdateCatalogItemHandler.Command(actor, id, "Nieuwe naam", null, null),
        "remove" => new RemoveCatalogItemHandler.Command(actor, id),
        "restore" => new RestoreCatalogItemHandler.Command(actor, id),
        _ => new MoveCatalogItemHandler.Command(actor, id, target),
    };

    [Fact]
    public async Task A_member_creates_a_category_for_their_family()
    {
        Result<CategoryDto, Exception> result = await SendAsync(new CreateCategoryHandler.Command(Member, "Toiletries"));

        result.Value.IsGeneral.ShouldBeFalse();
        harness.Categories.Verify(repository => repository.Add(It.Is<Category>(category => category.FamilyId == CatalogHarness.Family)), Times.Once);
    }

    [Fact]
    public async Task A_member_cannot_create_a_general_category()
    {
        Result<CategoryDto, Exception> result = await SendAsync(new CreateCategoryHandler.Command(Member, "Tent", General: true));

        result.Error.ShouldBeOfType<ForbiddenException>();
        harness.Categories.Verify(repository => repository.Add(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task An_administrator_creates_a_general_category_checked_against_general_names()
    {
        Result<CategoryDto, Exception> result = await SendAsync(new CreateCategoryHandler.Command(CatalogActor.Administrator, "Tent", General: true));

        result.Value.IsGeneral.ShouldBeTrue();
        harness.Categories.Verify(repository => repository.ExistsWithNameAsync(It.IsAny<CategoryName>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task An_administrator_without_a_family_cannot_create_an_own_category()
        => (await SendAsync(new CreateCategoryHandler.Command(CatalogActor.Administrator, "Eigen"))).Error.ShouldBeOfType<ForbiddenException>();

    [Theory]
    [MemberData(nameof(CategoryWrites))]
    public async Task Another_familys_category_is_not_found(string kind)
    {
        Category foreign = harness.GivenCategoryOfAnotherFamily();

        (await SendAsync(CategoryWrite(kind, Member, foreign.Id))).Error.ShouldBeOfType<NotFoundException>();
        harness.Categories.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(CategoryWrites))]
    public async Task A_member_cannot_change_a_general_category(string kind)
    {
        Category general = harness.GivenGeneralCategory();

        (await SendAsync(CategoryWrite(kind, Member, general.Id))).Error.ShouldBeOfType<ForbiddenException>();
        harness.Categories.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(CategoryWrites))]
    public async Task An_administrator_changes_a_general_category(string kind)
    {
        Category general = harness.GivenGeneralCategory();

        (await SendAsync(CategoryWrite(kind, CatalogActor.Administrator, general.Id))).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_member_cannot_add_an_item_to_a_general_category()
    {
        Category general = harness.GivenGeneralCategory();

        Result<CatalogItemDto, Exception> result = await SendAsync(new CreateCatalogItemHandler.Command(Member, general.Id, "Extra haringen", null, null));

        result.Error.ShouldBeOfType<ForbiddenException>();
        harness.Items.Verify(repository => repository.Add(It.IsAny<CatalogItem>()), Times.Never);
    }

    [Fact]
    public async Task An_administrator_adds_an_item_to_a_general_category()
    {
        Category general = harness.GivenGeneralCategory();

        (await SendAsync(new CreateCatalogItemHandler.Command(CatalogActor.Administrator, general.Id, "Hamer", null, null))).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_member_reads_the_items_of_a_general_category()
    {
        Category general = harness.GivenGeneralCategory();
        harness.Items
            .Setup(repository => repository.ListByCategoryAsync(general.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CatalogItem.Create(general, ItemName.Create("Haringen"), null)]);

        (await SendAsync(new ListCatalogItemsHandler.Query(Member, general.Id))).Value.Single().Name.ShouldBe("Haringen");
    }

    [Fact]
    public async Task Items_of_another_familys_category_are_not_found()
    {
        Category foreign = harness.GivenCategoryOfAnotherFamily();

        (await SendAsync(new ListCatalogItemsHandler.Query(Member, foreign.Id))).Error.ShouldBeOfType<NotFoundException>();
    }

    [Theory]
    [MemberData(nameof(ItemWrites))]
    public async Task An_item_of_another_family_is_not_found(string kind)
    {
        CatalogItem item = harness.GivenItem(harness.GivenCategoryOfAnotherFamily());

        (await SendAsync(ItemWrite(kind, Member, item.Id, harness.GivenCategory().Id))).Error.ShouldBeOfType<NotFoundException>();
        harness.Items.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ItemWrites))]
    public async Task A_member_cannot_change_a_general_item(string kind)
    {
        CatalogItem item = harness.GivenItem(harness.GivenGeneralCategory());

        (await SendAsync(ItemWrite(kind, Member, item.Id, harness.GivenGeneralCategory("Afwas").Id))).Error.ShouldBeOfType<ForbiddenException>();
        harness.Items.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task An_own_item_cannot_move_into_another_familys_category()
    {
        CatalogItem item = harness.GivenItem(harness.GivenCategory());

        Result<CatalogItemDto, Exception> result = await SendAsync(
            new MoveCatalogItemHandler.Command(Member, item.Id, harness.GivenCategoryOfAnotherFamily().Id));

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task An_administrator_cannot_move_a_general_item_into_an_own_category()
    {
        CatalogActor administratorInFamily = new(CatalogHarness.Family, true);
        CatalogItem item = harness.GivenItem(harness.GivenGeneralCategory());

        Result<CatalogItemDto, Exception> result = await SendAsync(
            new MoveCatalogItemHandler.Command(administratorInFamily, item.Id, harness.GivenCategory().Id));

        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Listing_shows_own_and_switched_on_general_categories_only()
    {
        Category own = Category.CreateForFamily(CatalogHarness.Family, CategoryName.Create("Eigen"));
        Category kept = Category.Create(CategoryName.Create("Tent"));
        Category off = Category.Create(CategoryName.Create("Kampeerkeuken"));
        harness.SwitchedOff.Add(off.Id);
        harness.Categories
            .Setup(repository => repository.ListVisibleAsync(CatalogHarness.Family, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([own, kept, off]);

        IReadOnlyList<CategoryDto> listed = (await SendAsync(new ListCategoriesHandler.Query(Member))).Value;
        IReadOnlyList<CategoryDto> all = (await SendAsync(new ListCategoriesHandler.Query(Member, IncludeSwitchedOff: true))).Value;

        listed.Select(category => category.Name).ShouldBe(["Eigen", "Tent"]);
        all.Single(category => category.Name == "Kampeerkeuken").IsSwitchedOn.ShouldBeFalse();
        all.Single(category => category.Name == "Tent").IsGeneral.ShouldBeTrue();
    }

    [Fact]
    public async Task Items_for_a_trip_skip_switched_off_and_foreign_categories()
    {
        Category own = harness.GivenCategory();
        Category off = harness.GivenGeneralCategory("Kampeerkeuken");
        Category foreign = harness.GivenCategoryOfAnotherFamily();
        harness.SwitchedOff.Add(off.Id);
        harness.Items
            .Setup(repository => repository.ListByCategoriesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await SendAsync(new ListCatalogItemsForCategoriesHandler.Query(Member, [own.Id, off.Id, foreign.Id]));

        harness.Items.Verify(
            repository => repository.ListByCategoriesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(own.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task A_member_switches_a_general_category_off()
    {
        Category general = harness.GivenGeneralCategory();

        Result<CategoryDto, Exception> result = await SendAsync(new SwitchGeneralCategoryHandler.Command(Member, general.Id, On: false));

        result.Value.IsSwitchedOn.ShouldBeFalse();
        harness.Categories.Verify(repository => repository.SwitchAsync(CatalogHarness.Family, general, false, It.IsAny<CancellationToken>()), Times.Once);
        harness.Categories.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task An_own_category_cannot_be_switched()
    {
        Category own = harness.GivenCategory();

        (await SendAsync(new SwitchGeneralCategoryHandler.Command(Member, own.Id, On: false))).Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task An_administrator_without_a_family_has_nothing_to_switch()
    {
        Category general = harness.GivenGeneralCategory();

        (await SendAsync(new SwitchGeneralCategoryHandler.Command(CatalogActor.Administrator, general.Id, On: false))).Error.ShouldBeOfType<ForbiddenException>();
    }

    [Fact]
    public async Task Only_an_administrator_lists_the_general_catalog()
    {
        harness.Categories
            .Setup(repository => repository.ListGeneralAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Category.Create(CategoryName.Create("Tent"))]);

        (await SendAsync(new ListGeneralCategoriesHandler.Query(CatalogActor.Administrator))).Value.Single().Name.ShouldBe("Tent");
        (await SendAsync(new ListGeneralCategoriesHandler.Query(Member))).Error.ShouldBeOfType<ForbiddenException>();
    }
}
