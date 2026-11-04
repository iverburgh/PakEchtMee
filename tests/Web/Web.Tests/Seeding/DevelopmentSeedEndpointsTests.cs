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
    private readonly HttpClient client = fixture.ClientForVerburgh();

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
    public async Task The_camping_trip_counts_per_head_items_for_verburgh()
    {
        TripDetailsResponse trip = await GetTripAsync(DevelopmentSeeder.CampingTripId);

        trip.Adults.ShouldBe(2);
        trip.Children.ShouldBe(3);
        TripItemResponse sleepingBag = trip.Items.Single(item => item.Name == "Slaapzak");
        sleepingBag.Basis.ShouldBe("PerChild");
        sleepingBag.Total.ShouldBe(3);
        trip.Items.Single(item => item.Name == "Borden").Total.ShouldBe(5);
        trip.Items.Single(item => item.Name == "Jas").Total.ShouldBe(2);
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

    [Fact]
    public async Task The_seeded_categories_are_general()
    {
        List<CatalogScopingEndpointsTests.CategoryResponse> categories =
            (await client.GetFromJsonAsync<List<CatalogScopingEndpointsTests.CategoryResponse>>("/api/categories", TestContext.Current.CancellationToken))!;

        categories.ShouldAllBe(category => category.IsGeneral);
    }

    [Fact]
    public async Task Another_family_sees_the_general_catalog_but_not_the_seeded_trips()
    {
        HttpClient other = await fixture.ClientForNewFamilyAsync();

        List<CatalogScopingEndpointsTests.CategoryResponse> categories =
            (await other.GetFromJsonAsync<List<CatalogScopingEndpointsTests.CategoryResponse>>("/api/categories", TestContext.Current.CancellationToken))!;
        List<IdOnly> trips = (await other.GetFromJsonAsync<List<IdOnly>>("/api/trips", TestContext.Current.CancellationToken))!;

        categories.Count.ShouldBe(22);
        trips.ShouldNotContain(trip => trip.Id == DevelopmentSeeder.CampingTripId);
        trips.ShouldNotContain(trip => trip.Id == DevelopmentSeeder.WinterSportsTripId);
    }
}
