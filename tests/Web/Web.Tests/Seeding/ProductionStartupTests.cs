using System.Net;
using System.Net.Http.Json;
using Catalog.Infrastructure.Sql.Seeding;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests.Seeding;

[IntegrationTest]
public sealed class ProductionStartupTests(ProductionApiFixture fixture) : IClassFixture<ProductionApiFixture>
{
    private readonly HttpClient client = fixture.ClientFor(ProductionApiFixture.VerburghAdministrator);

    [Fact]
    public async Task A_fresh_database_gets_the_general_catalog_but_no_trips()
    {
        List<CatalogScopingEndpointsTests.CategoryResponse> categories =
            (await client.GetFromJsonAsync<List<CatalogScopingEndpointsTests.CategoryResponse>>("/api/categories", TestContext.Current.CancellationToken))!;
        List<TripEndpointsTests.IdOnly> trips =
            (await client.GetFromJsonAsync<List<TripEndpointsTests.IdOnly>>("/api/trips", TestContext.Current.CancellationToken))!;

        categories.Select(category => category.Id).ShouldBe(CatalogSeedData.Categories.Select(category => category.Id), ignoreOrder: true);
        categories.ShouldAllBe(category => category.IsGeneral);
        trips.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_migration_creates_Verburgh_with_the_administrator_as_member()
    {
        MeResponse me = (await client.GetFromJsonAsync<MeResponse>("/api/me", TestContext.Current.CancellationToken))!;

        me.IsAdministrator.ShouldBeTrue();
        me.Family!.Name.ShouldBe("Verburgh");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_answer_without_a_token(string path)
    {
        HttpResponseMessage response = await fixture.ClientFor(null).GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed record MeResponse(string Email, bool IsAdministrator, FamilyResponse? Family);

    private sealed record FamilyResponse(Guid Id, string Name);
}
