using Catalog.Domain.Categories;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Categories;

public sealed class CategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    private static Category ACategory(string name = "Toiletries") => Category.Create(CategoryName.Create(name));

    [Fact]
    public void A_new_category_is_not_deleted()
    {
        Category category = ACategory();

        category.IsDeleted.ShouldBeFalse();
        category.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Renaming_replaces_the_name()
    {
        Category category = ACategory();

        category.Rename(CategoryName.Create("Bathroom"));

        category.Name.Value.ShouldBe("Bathroom");
    }

    [Fact]
    public void Removing_marks_the_category_as_deleted()
    {
        Category category = ACategory();

        category.Delete(Now);

        category.IsDeleted.ShouldBeTrue();
        category.DeletedAt.ShouldBe(Now);
    }

    [Fact]
    public void Removing_twice_keeps_the_first_moment()
    {
        Category category = ACategory();
        category.Delete(Now);

        category.Delete(Now.AddDays(1));

        category.DeletedAt.ShouldBe(Now);
    }

    [Fact]
    public void Restoring_brings_the_category_back()
    {
        Category category = ACategory();
        category.Delete(Now);

        category.Restore();

        category.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void A_removed_category_cannot_be_renamed()
    {
        Category category = ACategory();
        category.Delete(Now);

        Should.Throw<DomainValidationException>(() => category.Rename(CategoryName.Create("Bathroom")));
    }
}
