using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests;

/// <summary>The packing-catalog scenarios that involve families and the general catalog.</summary>
[IntegrationTest]
public sealed class CatalogScopingEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient memberA = fixture.ClientFor(ApiFixture.MemberOfA);
    private readonly HttpClient memberB = fixture.ClientFor(ApiFixture.MemberOfB);
    private readonly HttpClient administrator = fixture.ClientFor(ApiFixture.Administrator);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..(prefix.Length + 13)];

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string name, bool general = false)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/categories", new { Name = name, General = general }, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(CancellationToken))!;
    }

    private static async Task<List<CategoryResponse>> ListAsync(HttpClient client, bool includeSwitchedOff = false)
        => (await client.GetFromJsonAsync<List<CategoryResponse>>($"/api/categories?includeSwitchedOff={includeSwitchedOff}", CancellationToken))!;

    [Fact]
    public async Task A_familys_category_is_invisible_to_another_family()
    {
        CategoryResponse own = await CreateCategoryAsync(memberA, Unique("Toiletries"));

        own.IsGeneral.ShouldBeFalse();
        (await ListAsync(memberA)).ShouldContain(category => category.Id == own.Id);
        (await ListAsync(memberB)).ShouldNotContain(category => category.Id == own.Id);
        (await memberB.GetAsync($"/api/items?categoryId={own.Id}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await memberB.PutAsJsonAsync($"/api/categories/{own.Id}", new { Name = "Overgenomen" }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_same_name_is_allowed_in_each_scope()
    {
        string name = Unique("Toiletries");

        await CreateCategoryAsync(memberA, name);
        await CreateCategoryAsync(memberB, name);
        await CreateCategoryAsync(administrator, name, general: true);

        HttpResponseMessage duplicate = await memberA.PostAsJsonAsync("/api/categories", new { Name = name.ToLowerInvariant() }, CancellationToken);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_general_category_is_shared_read_only_with_every_family()
    {
        CategoryResponse general = await CreateCategoryAsync(administrator, Unique("Tent"), general: true);
        HttpResponseMessage added = await administrator.PostAsJsonAsync(
            "/api/items", new { CategoryId = general.Id, Name = "Hamer", Quantity = (int?)null, Unit = (string?)null }, CancellationToken);
        added.StatusCode.ShouldBe(HttpStatusCode.Created);

        (await ListAsync(memberA)).ShouldContain(category => category.Id == general.Id && category.IsGeneral);
        (await ListAsync(memberB)).ShouldContain(category => category.Id == general.Id);
        List<ItemResponse> items = (await memberB.GetFromJsonAsync<List<ItemResponse>>($"/api/items?categoryId={general.Id}", CancellationToken))!;
        items.ShouldContain(item => item.Name == "Hamer");

        (await memberA.PutAsJsonAsync($"/api/categories/{general.Id}", new { Name = "Mijn tent" }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await memberA.PostAsJsonAsync("/api/items", new { CategoryId = general.Id, Name = "Extra haringen" }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_member_cannot_create_a_general_category()
        => (await memberA.PostAsJsonAsync("/api/categories", new { Name = Unique("Tent"), General = true }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    [Fact]
    public async Task Switching_a_general_category_off_only_affects_that_family()
    {
        CategoryResponse general = await CreateCategoryAsync(administrator, Unique("Kampeerkeuken"), general: true);

        HttpResponseMessage off = await memberA.PutAsJsonAsync($"/api/categories/{general.Id}/switch", new { On = false }, CancellationToken);
        off.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await ListAsync(memberA)).ShouldNotContain(category => category.Id == general.Id);
        (await ListAsync(memberA, includeSwitchedOff: true)).ShouldContain(category => category.Id == general.Id && !category.IsSwitchedOn);
        (await ListAsync(memberB)).ShouldContain(category => category.Id == general.Id && category.IsSwitchedOn);

        await memberA.PutAsJsonAsync($"/api/categories/{general.Id}/switch", new { On = true }, CancellationToken);

        (await ListAsync(memberA)).ShouldContain(category => category.Id == general.Id);
    }

    [Fact]
    public async Task A_new_general_category_is_on_for_every_family()
    {
        CategoryResponse general = await CreateCategoryAsync(administrator, Unique("Skispullen"), general: true);

        (await ListAsync(memberA)).ShouldContain(category => category.Id == general.Id && category.IsSwitchedOn);
        (await ListAsync(memberB)).ShouldContain(category => category.Id == general.Id && category.IsSwitchedOn);
    }

    [Fact]
    public async Task Only_the_administrator_lists_the_general_catalog()
    {
        CategoryResponse general = await CreateCategoryAsync(administrator, Unique("Tent"), general: true);

        List<CategoryResponse> listed = (await administrator.GetFromJsonAsync<List<CategoryResponse>>("/api/categories/general", CancellationToken))!;
        listed.ShouldContain(category => category.Id == general.Id);
        (await memberA.GetAsync("/api/categories/general", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_administrator_without_a_family_sees_only_general_categories()
    {
        CategoryResponse own = await CreateCategoryAsync(memberA, Unique("Eigen"));

        (await ListAsync(administrator)).ShouldNotContain(category => category.Id == own.Id);
    }

    internal sealed record CategoryResponse(Guid Id, string Name, bool IsDeleted, bool IsGeneral, bool IsSwitchedOn);

    internal sealed record ItemResponse(Guid Id, Guid CategoryId, string Name);
}
