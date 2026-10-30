using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Web.Seeding;
using Xunit;
using static Web.Tests.TripEndpointsTests;

namespace Web.Tests.Seeding;

[IntegrationTest]
public sealed class DevelopmentSeedEndpointsTests(DevelopmentApiFixture fixture) : IClassFixture<DevelopmentApiFixture>
{
    private readonly HttpClient client = fixture.CreateClient();

    private async Task<TripDetailsResponse> GetTripAsync(Guid id)
        => (await client.GetFromJsonAsync<TripDetailsResponse>($"/api/trips/{id}", TestContext.Current.CancellationToken))!;

    [Fact]
    public async Task The_catalog_holds_the_seeded_categories()
    {
        List<CatalogEndpointsTests.CategoryResponse> categories =
            (await client.GetFromJsonAsync<List<CatalogEndpointsTests.CategoryResponse>>("/api/categories", TestContext.Current.CancellationToken))!;

        categories.Count.ShouldBe(22);
        categories.ShouldContain(category => category.Name == "Kinderen – Kleding");
        categories.ShouldContain(category => category.Name == "Kleding");
    }

    [Fact]
    public async Task The_camping_trip_holds_the_full_list()
    {
        TripDetailsResponse trip = await GetTripAsync(DevelopmentSeeder.CampingTripId);

        trip.Name.ShouldBe("Kamperen Frankrijk 2026");
        trip.StartDate.ShouldBeNull();
        trip.Categories.Count.ShouldBe(22);
        trip.Progress.Total.ShouldBe(149);
        trip.Progress.Active.ShouldBe(149);
    }

    [Fact]
    public async Task The_winter_sports_trip_leaves_out_camping_and_summer_items()
    {
        TripDetailsResponse trip = await GetTripAsync(DevelopmentSeeder.WinterSportsTripId);

        trip.Name.ShouldBe("Wintersport Frankrijk 2025");
        trip.Categories.Count.ShouldBe(16);
        trip.Categories.ShouldNotContain(category => category.Name == "Tent");
        trip.Progress.Total.ShouldBe(88);
        trip.Progress.Active.ShouldBe(88);

        TripDetailsResponse deleted = (await client.GetFromJsonAsync<TripDetailsResponse>(
            $"/api/trips/{DevelopmentSeeder.WinterSportsTripId}?status=Deleted",
            TestContext.Current.CancellationToken))!;
        deleted.Items.Count.ShouldBe(16);
        deleted.Items.ShouldContain(item => item.Name == "Zwembandjes");
        deleted.Items.ShouldContain(item => item.Name == "Slippers/crocs/teva's");
    }
}
