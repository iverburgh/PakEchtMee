using System.Net;
using System.Net.Http.Json;
using System.Text;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests;

[IntegrationTest]
public sealed class TripEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static StringContent NoBody => new("{}", Encoding.UTF8, "application/json");

    private readonly HttpClient client = fixture.CreateClient();

    private async Task<Guid> GivenCategoryWithItemsAsync(int itemCount)
    {
        Guid categoryId = (await (await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = $"Category {Guid.NewGuid():N}" },
            TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<IdOnly>(TestContext.Current.CancellationToken))!.Id;

        for (int index = 1; index <= itemCount; index++)
        {
            await client.PostAsJsonAsync(
                "/api/items",
                new { CategoryId = categoryId, Name = $"Item {index}", Quantity = (int?)null, Unit = (string?)null },
                TestContext.Current.CancellationToken);
        }

        return categoryId;
    }

    private async Task<Guid> GivenTripAsync(string? name = null, DateOnly? start = null, DateOnly? end = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/trips",
            new { Name = name ?? $"Trip {Guid.NewGuid():N}", StartDate = start, EndDate = end },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<IdOnly>(TestContext.Current.CancellationToken))!.Id;
    }

    [Fact]
    public async Task A_trip_with_an_end_before_the_start_returns_400()
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/trips",
            new { Name = "Broken", StartDate = new DateOnly(2026, 7, 18), EndDate = new DateOnly(2026, 7, 4) },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Selecting_categories_generates_the_packing_list()
    {
        Guid categoryId = await GivenCategoryWithItemsAsync(3);
        Guid tripId = await GivenTripAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/categories", new { CategoryIds = new[] { categoryId } }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TripDetailsResponse details = (await response.Content.ReadFromJsonAsync<TripDetailsResponse>(TestContext.Current.CancellationToken))!;
        details.Items.Count.ShouldBe(3);
        details.Items.ShouldAllBe(item => item.Status == "Active");
        details.Progress.Total.ShouldBe(3);
    }

    [Fact]
    public async Task An_item_walks_the_whole_flow_and_stops_at_loaded()
    {
        Guid categoryId = await GivenCategoryWithItemsAsync(1);
        Guid tripId = await GivenTripAsync();
        TripDetailsResponse details = (await (await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/categories",
            new { CategoryIds = new[] { categoryId } },
            TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<TripDetailsResponse>(TestContext.Current.CancellationToken))!;
        Guid itemId = details.Items[0].Id;

        foreach (string expected in new[] { "Prepared", "Packed", "Loaded" })
        {
            HttpResponseMessage advanced = await client.PostAsync(
                $"/api/trips/{tripId}/items/{itemId}/advance", NoBody, TestContext.Current.CancellationToken);

            advanced.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await advanced.Content.ReadFromJsonAsync<TripItemResponse>(TestContext.Current.CancellationToken))!.Status.ShouldBe(expected);
        }

        HttpResponseMessage tooFar = await client.PostAsync(
            $"/api/trips/{tripId}/items/{itemId}/advance", NoBody, TestContext.Current.CancellationToken);

        tooFar.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        TripDetailsResponse after = (await client.GetFromJsonAsync<TripDetailsResponse>(
            $"/api/trips/{tripId}", TestContext.Current.CancellationToken))!;
        after.Items[0].Status.ShouldBe("Loaded");
    }

    [Fact]
    public async Task Reverting_undoes_a_single_step()
    {
        Guid categoryId = await GivenCategoryWithItemsAsync(1);
        Guid tripId = await GivenTripAsync();
        TripDetailsResponse details = (await (await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/categories",
            new { CategoryIds = new[] { categoryId } },
            TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<TripDetailsResponse>(TestContext.Current.CancellationToken))!;
        Guid itemId = details.Items[0].Id;

        await client.PostAsync($"/api/trips/{tripId}/items/{itemId}/advance", NoBody, TestContext.Current.CancellationToken);
        await client.PostAsync($"/api/trips/{tripId}/items/{itemId}/advance", NoBody, TestContext.Current.CancellationToken);

        HttpResponseMessage reverted = await client.PostAsync(
            $"/api/trips/{tripId}/items/{itemId}/revert", NoBody, TestContext.Current.CancellationToken);

        (await reverted.Content.ReadFromJsonAsync<TripItemResponse>(TestContext.Current.CancellationToken))!.Status.ShouldBe("Prepared");
    }

    [Fact]
    public async Task Deleting_and_restoring_an_item_keeps_its_status()
    {
        Guid categoryId = await GivenCategoryWithItemsAsync(2);
        Guid tripId = await GivenTripAsync();
        TripDetailsResponse details = (await (await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/categories",
            new { CategoryIds = new[] { categoryId } },
            TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<TripDetailsResponse>(TestContext.Current.CancellationToken))!;
        Guid itemId = details.Items[0].Id;

        await client.PostAsync($"/api/trips/{tripId}/items/{itemId}/advance", NoBody, TestContext.Current.CancellationToken);
        await client.PostAsync($"/api/trips/{tripId}/items/{itemId}/delete", NoBody, TestContext.Current.CancellationToken);

        TripDetailsResponse afterDelete = (await client.GetFromJsonAsync<TripDetailsResponse>(
            $"/api/trips/{tripId}", TestContext.Current.CancellationToken))!;
        afterDelete.Progress.Total.ShouldBe(1);

        HttpResponseMessage restored = await client.PostAsync(
            $"/api/trips/{tripId}/items/{itemId}/restore", NoBody, TestContext.Current.CancellationToken);

        (await restored.Content.ReadFromJsonAsync<TripItemResponse>(TestContext.Current.CancellationToken))!.Status.ShouldBe("Prepared");
    }

    [Fact]
    public async Task Filtering_by_category_and_status_narrows_the_item_list()
    {
        Guid first = await GivenCategoryWithItemsAsync(2);
        Guid second = await GivenCategoryWithItemsAsync(2);
        Guid tripId = await GivenTripAsync();
        await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/categories", new { CategoryIds = new[] { first, second } }, TestContext.Current.CancellationToken);

        TripDetailsResponse all = (await client.GetFromJsonAsync<TripDetailsResponse>(
            $"/api/trips/{tripId}", TestContext.Current.CancellationToken))!;
        all.Items.Count.ShouldBe(4);

        TripDetailsResponse filtered = (await client.GetFromJsonAsync<TripDetailsResponse>(
            $"/api/trips/{tripId}?categoryId={first}&status=Active", TestContext.Current.CancellationToken))!;
        filtered.Items.Count.ShouldBe(2);
        filtered.Items.ShouldAllBe(item => item.CategoryId == first);
    }

    [Fact]
    public async Task An_unknown_trip_returns_404()
    {
        HttpResponseMessage response = await client.GetAsync(
            $"/api/trips/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    internal sealed record IdOnly(Guid Id);

    internal sealed record TripItemResponse(Guid Id, Guid CategoryId, string CategoryName, string Name, int? Quantity, string? Unit, string Status, string? NextStatus, string? PreviousStatus, bool IsDeleted);

    internal sealed record ProgressResponse(int Total, int Active, int Prepared, int Packed, int Loaded, double LoadedShare);

    internal sealed record TripCategoryResponse(Guid CategoryId, string Name, bool HasProgressedItems);

    internal sealed record TripDetailsResponse(
        Guid Id,
        string Name,
        DateOnly? StartDate,
        DateOnly? EndDate,
        IReadOnlyList<TripCategoryResponse> Categories,
        IReadOnlyList<TripItemResponse> Items,
        ProgressResponse Progress);
}
