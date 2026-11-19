using System.Net;
using System.Net.Http.Json;
using System.Text;
using Shared.Tests;
using Shouldly;
using Xunit;
using static Web.Tests.CatalogEndpointsTests;
using static Web.Tests.FamilyCompositionEndpointsTests;
using static Web.Tests.TripEndpointsTests;

namespace Web.Tests;

/// <summary>The quantity scenarios across contexts: catalog rule, family composition, trip totals and per-trip changes.</summary>
[IntegrationTest]
public sealed class QuantityEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private readonly HttpClient memberA = fixture.ClientFor(ApiFixture.MemberOfA);
    private readonly HttpClient memberB = fixture.ClientFor(ApiFixture.MemberOfB);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static StringContent NoBody => new("{}", Encoding.UTF8, "application/json");

    internal sealed record PlanChangedResponse(TripDetailsResponse Trip, int ChangedTotals);

    /// <summary>Family A is two adults and three children in every test of this class.</summary>
    public async ValueTask InitializeAsync()
        => (await memberA.PutAsJsonAsync("/api/family/composition", new { Adults = 2, Children = 3 }, CancellationToken)).EnsureSuccessStatusCode();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<Guid> CategoryAsync(params ItemInput[] items)
    {
        HttpResponseMessage created = await memberA.PostAsJsonAsync("/api/categories", new { Name = $"Buitenkleding {Guid.NewGuid():N}" }, CancellationToken);
        Guid categoryId = (await created.Content.ReadFromJsonAsync<IdOnly>(CancellationToken))!.Id;

        foreach (ItemInput item in items)
        {
            HttpResponseMessage response = await memberA.PostAsJsonAsync("/api/items", item.WithCategory(categoryId), CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        return categoryId;
    }

    private async Task<TripDetailsResponse> TripWithAsync(Guid categoryId, DateOnly? start = null, DateOnly? end = null)
    {
        HttpResponseMessage created = await memberA.PostAsJsonAsync(
            "/api/trips", new { Name = $"Uitje {Guid.NewGuid():N}", StartDate = start, EndDate = end }, CancellationToken);
        Guid tripId = (await created.Content.ReadFromJsonAsync<IdOnly>(CancellationToken))!.Id;
        HttpResponseMessage selected = await memberA.PostAsJsonAsync($"/api/trips/{tripId}/categories", new { CategoryIds = new[] { categoryId } }, CancellationToken);

        return (await selected.Content.ReadFromJsonAsync<TripDetailsResponse>(CancellationToken))!;
    }

    [Fact]
    public async Task A_catalog_item_keeps_its_basis()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Jassen", 1, "PerChild"));

        List<ItemResponse> items = (await memberA.GetFromJsonAsync<List<ItemResponse>>($"/api/items?categoryId={categoryId}", CancellationToken))!;

        items.Single().Basis.ShouldBe("PerChild");
        items.Single().Quantity.ShouldBe(1);
    }

    [Fact]
    public async Task A_catalog_item_with_an_unknown_basis_is_rejected()
    {
        Guid categoryId = await CategoryAsync();

        HttpResponseMessage response = await memberA.PostAsJsonAsync(
            "/api/items", new { CategoryId = categoryId, Name = "Jassen", Quantity = 1, Basis = "PerDog" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_new_trip_takes_over_the_family_composition_and_counts_per_head()
    {
        Guid categoryId = await CategoryAsync(
            new ItemInput("Jassen", 1, "PerChild"),
            new ItemInput("Borden", 1, "PerPerson"),
            new ItemInput("Haringen", 20, null),
            new ItemInput("Paraplu", null, null));

        TripDetailsResponse trip = await TripWithAsync(categoryId);

        trip.Adults.ShouldBe(2);
        trip.Children.ShouldBe(3);
        trip.Items.Single(item => item.Name == "Jassen").Total.ShouldBe(3);
        trip.Items.Single(item => item.Name == "Borden").Total.ShouldBe(5);
        trip.Items.Single(item => item.Name == "Haringen").Total.ShouldBe(20);
        trip.Items.Single(item => item.Name == "Paraplu").Total.ShouldBeNull();
    }

    [Fact]
    public async Task Changing_a_trips_composition_recalculates_only_items_still_to_do()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Jassen", 1, "PerChild"), new ItemInput("Schoenen", 1, "PerChild"));
        TripDetailsResponse trip = await TripWithAsync(categoryId);
        Guid shoes = trip.Items.Single(item => item.Name == "Schoenen").Id;
        await memberA.PostAsync($"/api/trips/{trip.Id}/items/{shoes}/advance", NoBody, CancellationToken);

        HttpResponseMessage response = await memberA.PutAsJsonAsync($"/api/trips/{trip.Id}/plan", new { Adults = 2, Children = 2 }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PlanChangedResponse changed = (await response.Content.ReadFromJsonAsync<PlanChangedResponse>(CancellationToken))!;
        changed.ChangedTotals.ShouldBe(1);
        changed.Trip.Children.ShouldBe(2);
        changed.Trip.Items.Single(item => item.Name == "Jassen").Total.ShouldBe(2);
        changed.Trip.Items.Single(item => item.Name == "Schoenen").Total.ShouldBe(3);

        CompositionResponse? family = await memberA.GetFromJsonAsync<CompositionResponse>("/api/family/composition", CancellationToken);
        family.ShouldBe(new CompositionResponse(2, 3));
    }

    [Fact]
    public async Task A_catalog_item_keeps_its_period()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Sokken", 1, "PerChild", "PerDay"));

        List<ItemResponse> items = (await memberA.GetFromJsonAsync<List<ItemResponse>>($"/api/items?categoryId={categoryId}", CancellationToken))!;

        items.Single().Period.ShouldBe("PerDay");
    }

    [Fact]
    public async Task A_two_week_trip_counts_socks_per_child_per_day()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Sokken", 1, "PerChild", "PerDay"));

        TripDetailsResponse trip = await TripWithAsync(categoryId, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 18));

        trip.Days.ShouldBe(15);
        trip.Nights.ShouldBe(14);
        trip.Items.Single().Total.ShouldBe(45);
        trip.Items.Single().Period.ShouldBe("PerDay");
    }

    [Fact]
    public async Task A_longer_trip_recalculates_per_day_items()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Sokken", 1, "PerChild", "PerDay"));
        TripDetailsResponse trip = await TripWithAsync(categoryId, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 18));

        HttpResponseMessage response = await memberA.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/plan",
            new { Adults = 2, Children = 3, StartDate = new DateOnly(2026, 7, 4), EndDate = new DateOnly(2026, 7, 25) },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PlanChangedResponse changed = (await response.Content.ReadFromJsonAsync<PlanChangedResponse>(CancellationToken))!;
        changed.ChangedTotals.ShouldBe(1);
        changed.Trip.Days.ShouldBe(22);
        changed.Trip.Nights.ShouldBe(21);
        changed.Trip.Items.Single().Total.ShouldBe(66);
    }

    [Fact]
    public async Task A_trip_without_dates_counts_per_day_items_once()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Sokken", 1, "PerChild", "PerDay")));

        trip.Days.ShouldBeNull();
        trip.Items.Single().Total.ShouldBe(3);
    }

    [Fact]
    public async Task An_end_date_before_the_start_date_is_rejected()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Sokken", 1, "PerChild", "PerDay")));

        HttpResponseMessage response = await memberA.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/plan",
            new { Adults = 2, Children = 3, StartDate = new DateOnly(2026, 7, 18), EndDate = new DateOnly(2026, 7, 4) },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Changing_an_items_quantity_to_per_day_counts_the_days()
    {
        Guid categoryId = await CategoryAsync(new ItemInput("Onderbroeken", null, null));
        TripDetailsResponse trip = await TripWithAsync(categoryId, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 18));

        HttpResponseMessage response = await memberA.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/items/{trip.Items.Single().Id}/quantity",
            new { Quantity = 1, Basis = "PerAdult", Period = "PerDay" },
            CancellationToken);

        TripItemResponse item = (await response.Content.ReadFromJsonAsync<TripItemResponse>(CancellationToken))!;
        item.Total.ShouldBe(30);
        item.Period.ShouldBe("PerDay");
    }

    [Fact]
    public async Task An_invalid_trip_composition_is_rejected()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Jassen", 1, "PerChild")));

        HttpResponseMessage response = await memberA.PutAsJsonAsync($"/api/trips/{trip.Id}/plan", new { Adults = 0, Children = 0 }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Changing_an_items_quantity_recalculates_its_total()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Jassen", 1, "PerChild")));
        Guid jackets = trip.Items.Single().Id;

        HttpResponseMessage response = await memberA.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/items/{jackets}/quantity", new { Quantity = 2, Unit = (string?)null, Basis = "PerChild" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TripItemResponse item = (await response.Content.ReadFromJsonAsync<TripItemResponse>(CancellationToken))!;
        item.Total.ShouldBe(6);
        item.Basis.ShouldBe("PerChild");
    }

    [Fact]
    public async Task The_quantity_of_a_packed_item_cannot_be_changed()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Jassen", 1, "PerChild")));
        Guid jackets = trip.Items.Single().Id;
        await memberA.PostAsync($"/api/trips/{trip.Id}/items/{jackets}/advance", NoBody, CancellationToken);
        await memberA.PostAsync($"/api/trips/{trip.Id}/items/{jackets}/advance", NoBody, CancellationToken);

        HttpResponseMessage response = await memberA.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/items/{jackets}/quantity", new { Quantity = 2, Basis = "PerChild" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Another_familys_trip_is_not_found()
    {
        TripDetailsResponse trip = await TripWithAsync(await CategoryAsync(new ItemInput("Jassen", 1, "PerChild")));
        Guid jackets = trip.Items.Single().Id;

        HttpResponseMessage composition = await memberB.PutAsJsonAsync($"/api/trips/{trip.Id}/plan", new { Adults = 1, Children = 1 }, CancellationToken);
        HttpResponseMessage quantity = await memberB.PutAsJsonAsync(
            $"/api/trips/{trip.Id}/items/{jackets}/quantity", new { Quantity = 9 }, CancellationToken);

        composition.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        quantity.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

/// <summary>An item to add to a test category; <see cref="WithCategory"/> turns it into the request body.</summary>
internal sealed record ItemInput(string Name, int? Quantity, string? Basis, string? Period = null)
{
    public object WithCategory(Guid categoryId) => new { CategoryId = categoryId, Name, Quantity, Unit = (string?)null, Basis, Period };
}
