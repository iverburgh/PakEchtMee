using System.Globalization;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Catalog.Infrastructure.Sql.Seeding;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests.Seeding;

public sealed class CatalogSeedDataTests
{
    /// <summary>Same rules as the database collation, so a duplicate here would be a unique-index clash there.</summary>
    private static readonly StringComparer NameComparer =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

    private static SeedCategory CategoryNamed(string name) => CatalogSeedData.Categories.Single(category => category.Name == name);

    [Fact]
    public void The_seed_holds_22_categories_with_149_items()
    {
        CatalogSeedData.Categories.Count.ShouldBe(22);
        CatalogSeedData.Categories.Sum(category => category.Items.Count).ShouldBe(149);
    }

    [Fact]
    public void Category_names_are_unique()
        => CatalogSeedData.Categories.Select(category => category.Name).Distinct(NameComparer).Count().ShouldBe(22);

    [Fact]
    public void Item_names_are_unique_within_their_category()
        => CatalogSeedData.Categories.ShouldAllBe(category =>
            category.Items.Select(item => item.Name).Distinct(NameComparer).Count() == category.Items.Count);

    [Fact]
    public void Names_fit_the_catalog_limits()
    {
        CatalogSeedData.Categories.ShouldAllBe(category => category.Name.Length <= CategoryName.MaxLength);
        CatalogSeedData.Categories.SelectMany(category => category.Items).ShouldAllBe(item => item.Name.Length <= ItemName.MaxLength);
    }

    [Fact]
    public void Ids_are_unique()
    {
        CatalogSeedData.Categories.Select(category => category.Id).Distinct().Count().ShouldBe(22);
        CatalogSeedData.Categories.SelectMany(category => category.Items).Select(item => item.Id).Distinct().Count().ShouldBe(149);
    }

    [Fact]
    public void Items_without_a_sub_heading_belong_to_kinderen_algemeen()
        => CategoryNamed(CatalogSeedData.KinderenAlgemeen).Items.Select(item => item.Name)
            .ShouldBe(["Snoetenpoetsers", "Extra beddengoed", "Dekens"]);

    [Theory]
    [InlineData(CatalogSeedData.KinderenSlapen, "Slaapzak", QuantityBasis.PerChild)]
    [InlineData(CatalogSeedData.KinderenSlapen, "Matje", QuantityBasis.PerChild)]
    [InlineData(CatalogSeedData.KinderenSlapen, "Kussen", QuantityBasis.PerChild)]
    [InlineData(CatalogSeedData.KinderenToiletartikelen, "Handdoek(en) douchen", QuantityBasis.PerChild)]
    [InlineData(CatalogSeedData.KinderenBuitenkleding, "Jassen", QuantityBasis.PerChild)]
    [InlineData(CatalogSeedData.Buitenkleding, "Jas", QuantityBasis.PerAdult)]
    [InlineData(CatalogSeedData.SlapenInTent, "Matjes", QuantityBasis.PerAdult)]
    [InlineData(CatalogSeedData.Kampeerkeuken, "Borden", QuantityBasis.PerPerson)]
    [InlineData(CatalogSeedData.KamperenAlgemeen, "Stoelen", QuantityBasis.PerPerson)]
    public void Per_head_items_carry_one_per_head(string category, string name, QuantityBasis basis)
    {
        SeedItem item = CategoryNamed(category).Items.Single(item => item.Name == name);

        item.Quantity.ShouldBe(1);
        item.Basis.ShouldBe(basis);
    }

    [Fact]
    public void Every_per_head_entry_names_an_existing_seed_item()
        => CatalogSeedData.PerHead.Keys.ShouldAllBe(key => CategoryNamed(key.Category).Items.Any(item => item.Name == key.Item));

    [Fact]
    public void Only_the_per_head_items_carry_a_quantity()
    {
        List<SeedItem> withQuantity = [.. CatalogSeedData.Categories.SelectMany(category => category.Items).Where(item => item.Quantity is not null)];

        withQuantity.Count.ShouldBe(CatalogSeedData.PerHead.Count);
        CategoryNamed(CatalogSeedData.Kampeerkeuken).Items.Single(item => item.Name == "Pannen").Quantity.ShouldBeNull();
    }

    [Fact]
    public void Ids_derive_from_the_seed_names()
    {
        CategoryNamed(CatalogSeedData.Tent).Id.ShouldBe(CatalogSeedData.CategoryId(CatalogSeedData.Tent));
        CategoryNamed(CatalogSeedData.Tent).Items[0].Id.ShouldBe(CatalogSeedData.ItemId(CatalogSeedData.Tent, "Tent + pomp"));
    }

    [Fact]
    public void Camping_categories_are_seeded_categories()
        => CatalogSeedData.CampingCategories.ShouldAllBe(name => CatalogSeedData.Categories.Any(category => category.Name == name));
}
