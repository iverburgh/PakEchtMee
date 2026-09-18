using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests;

[IntegrationTest]
public sealed class CatalogEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient Client => fixture.CreateClient();

    private static StringContent NoBody => new("{}", System.Text.Encoding.UTF8, "application/json");

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string? name = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = name ?? $"Category {Guid.NewGuid():N}" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task Creating_a_category_returns_201_with_a_location()
    {
        HttpClient client = Client;

        CategoryResponse created = await CreateCategoryAsync(client);

        created.Id.ShouldNotBe(Guid.Empty);
        created.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Creating_a_category_without_a_name_returns_a_problem_details_400()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/categories", new { Name = "  " }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Creating_a_duplicate_category_returns_400()
    {
        HttpClient client = Client;
        string name = $"Duplicate {Guid.NewGuid():N}";
        await CreateCategoryAsync(client, name);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/categories", new { Name = name.ToUpperInvariant() }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Renaming_an_unknown_category_returns_404()
    {
        HttpResponseMessage response = await Client.PutAsJsonAsync(
            $"/api/categories/{Guid.CreateVersion7()}", new { Name = "Whatever" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_category_can_be_removed_and_restored()
    {
        HttpClient client = Client;
        CategoryResponse created = await CreateCategoryAsync(client);

        HttpResponseMessage removed = await client.DeleteAsync($"/api/categories/{created.Id}", TestContext.Current.CancellationToken);
        removed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await removed.Content.ReadFromJsonAsync<CategoryResponse>(TestContext.Current.CancellationToken))!.IsDeleted.ShouldBeTrue();

        HttpResponseMessage restored = await client.PostAsync($"/api/categories/{created.Id}/restore", NoBody, TestContext.Current.CancellationToken);
        restored.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await restored.Content.ReadFromJsonAsync<CategoryResponse>(TestContext.Current.CancellationToken))!.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task An_item_is_created_with_its_amount_and_listed_under_its_category()
    {
        HttpClient client = Client;
        CategoryResponse category = await CreateCategoryAsync(client);

        HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/items",
            new { CategoryId = category.Id, Name = "Socks", Quantity = 7, Unit = "pair" },
            TestContext.Current.CancellationToken);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        ItemResponse[] items = (await client.GetFromJsonAsync<ItemResponse[]>(
            $"/api/items?categoryId={category.Id}", TestContext.Current.CancellationToken))!;

        items.Single().Quantity.ShouldBe(7);
        items.Single().Unit.ShouldBe("pair");
    }

    [Fact]
    public async Task An_item_with_a_unit_but_no_quantity_returns_400()
    {
        HttpClient client = Client;
        CategoryResponse category = await CreateCategoryAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/items",
            new { CategoryId = category.Id, Name = "Socks", Quantity = (int?)null, Unit = "pair" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_item_can_be_moved_to_another_category()
    {
        HttpClient client = Client;
        CategoryResponse source = await CreateCategoryAsync(client);
        CategoryResponse target = await CreateCategoryAsync(client);

        ItemResponse item = (await (await client.PostAsJsonAsync(
            "/api/items",
            new { CategoryId = source.Id, Name = "Charger", Quantity = (int?)null, Unit = (string?)null },
            TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<ItemResponse>(TestContext.Current.CancellationToken))!;

        HttpResponseMessage moved = await client.PostAsJsonAsync(
            $"/api/items/{item.Id}/move", new { TargetCategoryId = target.Id }, TestContext.Current.CancellationToken);

        moved.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await moved.Content.ReadFromJsonAsync<ItemResponse>(TestContext.Current.CancellationToken))!.CategoryId.ShouldBe(target.Id);
    }

    internal sealed record CategoryResponse(Guid Id, string Name, bool IsDeleted);

    internal sealed record ItemResponse(Guid Id, Guid CategoryId, string Name, int? Quantity, string? Unit, bool IsDeleted);
}
