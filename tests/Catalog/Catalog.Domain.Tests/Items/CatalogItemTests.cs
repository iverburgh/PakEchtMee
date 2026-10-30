using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Items;

public sealed class CatalogItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    private static Category ACategory(string name = "Toiletries") => Category.Create(CategoryName.Create(name));

    private static CatalogItem AnItem(Category category, string name = "Toothbrush", int? quantity = null, string? unit = null)
        => CatalogItem.Create(category, ItemName.Create(name), Amount.Create(quantity, unit));

    [Fact]
    public void A_new_item_belongs_to_its_category()
    {
        Category category = ACategory();

        CatalogItem item = AnItem(category);

        item.CategoryId.ShouldBe(category.Id);
        item.IsDeleted.ShouldBeFalse();
        item.Amount.ShouldBeNull();
    }

    [Fact]
    public void An_item_created_with_an_id_keeps_that_id()
    {
        Guid id = Guid.CreateVersion7();

        CatalogItem item = CatalogItem.Create(id, ACategory(), ItemName.Create("Toothbrush"), null);

        item.Id.ShouldBe(id);
    }

    [Fact]
    public void An_item_cannot_be_created_with_an_empty_id()
        => Should.Throw<ArgumentException>(() => CatalogItem.Create(Guid.Empty, ACategory(), ItemName.Create("Toothbrush"), null));

    [Fact]
    public void An_item_with_an_id_cannot_be_created_in_a_removed_category()
    {
        Category category = ACategory();
        category.Delete(Now);

        Should.Throw<DomainValidationException>(() => CatalogItem.Create(Guid.CreateVersion7(), category, ItemName.Create("Toothbrush"), null));
    }

    [Fact]
    public void An_item_can_carry_a_quantity_and_unit()
    {
        CatalogItem item = AnItem(ACategory(), "Socks", 7, "pair");

        item.Amount!.ToString().ShouldBe("7 pair");
    }

    [Fact]
    public void An_item_cannot_be_created_in_a_removed_category()
    {
        Category category = ACategory();
        category.Delete(Now);

        Should.Throw<DomainValidationException>(() => AnItem(category));
    }

    [Fact]
    public void Moving_changes_the_category()
    {
        Category source = ACategory("Electronics");
        Category target = ACategory("Travel documents");
        CatalogItem item = AnItem(source, "Charger");

        item.MoveTo(source, target);

        item.CategoryId.ShouldBe(target.Id);
    }

    [Fact]
    public void An_item_moves_between_categories_of_the_same_family()
    {
        Guid family = Guid.CreateVersion7();
        Category source = Category.CreateForFamily(family, CategoryName.Create("Electronics"));
        Category target = Category.CreateForFamily(family, CategoryName.Create("Travel documents"));
        CatalogItem item = AnItem(source, "Charger");

        item.MoveTo(source, target);

        item.CategoryId.ShouldBe(target.Id);
    }

    [Fact]
    public void A_general_item_cannot_move_into_a_family_category()
    {
        Category general = ACategory("Tent");
        Category own = Category.CreateForFamily(Guid.CreateVersion7(), CategoryName.Create("Eigen spullen"));
        CatalogItem item = AnItem(general, "Haringen");

        Should.Throw<DomainValidationException>(() => item.MoveTo(general, own));
        item.CategoryId.ShouldBe(general.Id);
    }

    [Fact]
    public void Moving_from_a_category_the_item_is_not_in_is_a_programming_error()
    {
        CatalogItem item = AnItem(ACategory("Electronics"), "Charger");

        Should.Throw<ArgumentException>(() => item.MoveTo(ACategory("Other"), ACategory("Travel documents")));
    }

    [Fact]
    public void An_item_cannot_be_moved_into_a_removed_category()
    {
        Category target = ACategory("Travel documents");
        target.Delete(Now);
        Category source = ACategory("Electronics");
        CatalogItem item = AnItem(source, "Charger");

        Should.Throw<DomainValidationException>(() => item.MoveTo(source, target));
    }

    [Fact]
    public void Renaming_replaces_the_name()
    {
        CatalogItem item = AnItem(ACategory());

        item.Rename(ItemName.Create("Electric toothbrush"));

        item.Name.Value.ShouldBe("Electric toothbrush");
    }

    [Fact]
    public void Changing_the_amount_replaces_it()
    {
        CatalogItem item = AnItem(ACategory(), "Towel");

        item.ChangeAmount(Amount.Create(2, null));

        item.Amount!.Quantity.ShouldBe(2);
    }

    [Fact]
    public void Removing_and_restoring_an_item()
    {
        CatalogItem item = AnItem(ACategory());

        item.Delete(Now);
        item.IsDeleted.ShouldBeTrue();

        item.Restore();
        item.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void A_removed_item_cannot_be_changed()
    {
        Category category = ACategory();
        CatalogItem item = AnItem(category);
        item.Delete(Now);

        Should.Throw<DomainValidationException>(() => item.Rename(ItemName.Create("Other")));
        Should.Throw<DomainValidationException>(() => item.ChangeAmount(Amount.Create(1, null)));
        Should.Throw<DomainValidationException>(() => item.MoveTo(category, ACategory("Other")));
    }
}
