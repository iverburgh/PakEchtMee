using Catalog.Infrastructure.Sql.Seeding;
using Shouldly;
using Web.Seeding;
using Xunit;

namespace Web.Tests.Seeding;

public sealed class DevelopmentSeederTests
{
    private static IEnumerable<Guid> AllSeedItemIds
        => CatalogSeedData.Categories.SelectMany(category => category.Items).Select(item => item.Id);

    [Fact]
    public void Winter_sports_drops_exactly_16_known_summer_items()
    {
        DevelopmentSeeder.WinterSportsSummerItemIds.Distinct().Count().ShouldBe(16);
        DevelopmentSeeder.WinterSportsSummerItemIds.ShouldAllBe(id => AllSeedItemIds.Contains(id));
    }

    [Fact]
    public void Summer_items_belong_to_categories_the_winter_sports_trip_selects()
    {
        HashSet<Guid> selectedItemIds =
        [
            .. CatalogSeedData.Categories
                .Where(category => DevelopmentSeeder.WinterSportsCategoryIds.Contains(category.Id))
                .SelectMany(category => category.Items)
                .Select(item => item.Id),
        ];

        DevelopmentSeeder.WinterSportsSummerItemIds.ShouldAllBe(id => selectedItemIds.Contains(id));
    }

    [Fact]
    public void Winter_sports_selects_the_16_non_camping_categories()
    {
        DevelopmentSeeder.WinterSportsCategoryIds.Count.ShouldBe(16);
        DevelopmentSeeder.WinterSportsCategoryIds.ShouldNotContain(CatalogSeedData.CategoryId(CatalogSeedData.Tent));
    }

    [Fact]
    public void Camping_selects_every_seeded_category()
        => DevelopmentSeeder.CampingCategoryIds.Count.ShouldBe(22);

    [Fact]
    public void The_two_trips_have_distinct_ids()
        => DevelopmentSeeder.CampingTripId.ShouldNotBe(DevelopmentSeeder.WinterSportsTripId);
}
