using Catalog.Domain.Categories;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Categories;

public sealed class CategoryAccessTests
{
    private static readonly Guid Verburgh = Guid.CreateVersion7();
    private static readonly Guid Jansen = Guid.CreateVersion7();

    private static Category General() => Category.Create(CategoryName.Create("Tent"));

    private static Category OwnOf(Guid familyId) => Category.CreateForFamily(familyId, CategoryName.Create("Eigen spullen"));

    [Fact]
    public void A_general_category_belongs_to_no_family()
    {
        Category category = General();

        category.IsGeneral.ShouldBeTrue();
        category.FamilyId.ShouldBeNull();
    }

    [Fact]
    public void A_family_category_belongs_to_its_family()
    {
        Category category = OwnOf(Verburgh);

        category.IsGeneral.ShouldBeFalse();
        category.FamilyId.ShouldBe(Verburgh);
    }

    [Fact]
    public void A_family_category_needs_a_family()
        => Should.Throw<ArgumentException>(() => Category.CreateForFamily(Guid.Empty, CategoryName.Create("Eigen spullen")));

    [Fact]
    public void A_family_edits_its_own_category()
        => OwnOf(Verburgh).AccessFor(CatalogActor.MemberOf(Verburgh)).ShouldBe(CatalogAccess.Edit);

    [Fact]
    public void Another_family_does_not_see_the_category()
        => OwnOf(Verburgh).AccessFor(CatalogActor.MemberOf(Jansen)).ShouldBe(CatalogAccess.None);

    [Fact]
    public void An_administrator_without_the_family_does_not_see_its_category()
        => OwnOf(Verburgh).AccessFor(CatalogActor.Administrator).ShouldBe(CatalogAccess.None);

    [Fact]
    public void A_member_reads_a_general_category()
        => General().AccessFor(CatalogActor.MemberOf(Verburgh)).ShouldBe(CatalogAccess.Read);

    [Fact]
    public void An_administrator_edits_a_general_category()
        => General().AccessFor(CatalogActor.Administrator).ShouldBe(CatalogAccess.Edit);

    [Fact]
    public void An_administrator_in_a_family_edits_both_general_and_own_categories()
    {
        CatalogActor actor = new(Verburgh, true);

        General().AccessFor(actor).ShouldBe(CatalogAccess.Edit);
        OwnOf(Verburgh).AccessFor(actor).ShouldBe(CatalogAccess.Edit);
        OwnOf(Jansen).AccessFor(actor).ShouldBe(CatalogAccess.None);
    }

    [Fact]
    public void Someone_without_family_or_role_sees_nothing()
        => General().AccessFor(new CatalogActor(null, false)).ShouldBe(CatalogAccess.None);
}
