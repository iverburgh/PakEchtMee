using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;
using static Web.Tests.TripEndpointsTests;

namespace Web.Tests;

/// <summary>The trip-packing scenarios that involve families and the general catalog.</summary>
[IntegrationTest]
public sealed class TripScopingEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient memberA = fixture.ClientFor(ApiFixture.MemberOfA);
    private readonly HttpClient memberB = fixture.ClientFor(ApiFixture.MemberOfB);
    private readonly HttpClient administrator = fixture.ClientFor(ApiFixture.Administrator);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<Guid> CategoryWithItemsAsync(HttpClient client, int itemCount, bool general = false)
    {
        HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/categories", new { Name = $"Categorie {Guid.NewGuid():N}", General = general }, CancellationToken);
        Guid categoryId = (await created.Content.ReadFromJsonAsync<IdOnly>(CancellationToken))!.Id;

        for (int index = 1; index <= itemCount; index++)
            await client.PostAsJsonAsync("/api/items", new { CategoryId = categoryId, Name = $"Item {index}" }, CancellationToken);

        return categoryId;
    }

    private static async Task<Guid> TripAsync(HttpClient client)
    {
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/trips", new { Name = $"Uitje {Guid.NewGuid():N}" }, CancellationToken);

        return (await created.Content.ReadFromJsonAsync<IdOnly>(CancellationToken))!.Id;
    }

    private static async Task<TripDetailsResponse> SelectAsync(HttpClient client, Guid tripId, params Guid[] categoryIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/trips/{tripId}/categories", new { CategoryIds = categoryIds }, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<TripDetailsResponse>(CancellationToken))!;
    }

    [Fact]
    public async Task A_trip_mixes_own_and_general_categories()
    {
        Guid own = await CategoryWithItemsAsync(memberA, 4);
        Guid general = await CategoryWithItemsAsync(administrator, 6, general: true);
        Guid tripId = await TripAsync(memberA);

        TripDetailsResponse trip = await SelectAsync(memberA, tripId, own, general);

        trip.Items.Count.ShouldBe(10);
        trip.Items.ShouldAllBe(item => item.Status == "Active");
    }

    [Fact]
    public async Task A_switched_off_general_category_adds_nothing()
    {
        Guid own = await CategoryWithItemsAsync(memberA, 2);
        Guid general = await CategoryWithItemsAsync(administrator, 3, general: true);
        await memberA.PutAsJsonAsync($"/api/categories/{general}/switch", new { On = false }, CancellationToken);
        Guid tripId = await TripAsync(memberA);

        TripDetailsResponse trip = await SelectAsync(memberA, tripId, own, general);

        trip.Items.Count.ShouldBe(2);
        trip.Categories.ShouldNotContain(category => category.CategoryId == general);
    }

    [Fact]
    public async Task Another_familys_category_adds_nothing()
    {
        Guid own = await CategoryWithItemsAsync(memberA, 2);
        Guid foreign = await CategoryWithItemsAsync(memberB, 5);
        Guid tripId = await TripAsync(memberA);

        TripDetailsResponse trip = await SelectAsync(memberA, tripId, own, foreign);

        trip.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Another_familys_trip_is_not_found_and_stays_unchanged()
    {
        Guid own = await CategoryWithItemsAsync(memberA, 1);
        Guid tripId = await TripAsync(memberA);
        TripDetailsResponse trip = await SelectAsync(memberA, tripId, own);
        Guid itemId = trip.Items[0].Id;

        (await memberB.GetAsync($"/api/trips/{tripId}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await memberB.PostAsync($"/api/trips/{tripId}/items/{itemId}/advance", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        TripDetailsResponse reloaded = (await memberA.GetFromJsonAsync<TripDetailsResponse>($"/api/trips/{tripId}", CancellationToken))!;
        reloaded.Items[0].Status.ShouldBe("Active");
    }

    [Fact]
    public async Task The_trip_list_only_shows_the_familys_trips()
    {
        Guid ours = await TripAsync(memberA);
        Guid theirs = await TripAsync(memberB);

        List<IdOnly> listed = (await memberA.GetFromJsonAsync<List<IdOnly>>("/api/trips", CancellationToken))!;

        listed.ShouldContain(trip => trip.Id == ours);
        listed.ShouldNotContain(trip => trip.Id == theirs);
    }

    [Fact]
    public async Task An_administrator_without_a_family_cannot_create_a_trip()
        => (await administrator.PostAsJsonAsync("/api/trips", new { Name = "Beheerdersuitje" }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
}
