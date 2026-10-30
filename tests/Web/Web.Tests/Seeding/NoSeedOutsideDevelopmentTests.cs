using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests.Seeding;

/// <summary>Uses its own <see cref="ApiFixture"/>, so the database is fresh and only startup could have filled it.</summary>
[IntegrationTest]
public sealed class NoSeedOutsideDevelopmentTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient client = fixture.ClientFor(ApiFixture.MemberOfA);

    [Fact]
    public async Task A_fresh_database_stays_empty_outside_development()
    {
        List<CatalogEndpointsTests.CategoryResponse> categories =
            (await client.GetFromJsonAsync<List<CatalogEndpointsTests.CategoryResponse>>("/api/categories", TestContext.Current.CancellationToken))!;
        List<TripEndpointsTests.IdOnly> trips =
            (await client.GetFromJsonAsync<List<TripEndpointsTests.IdOnly>>("/api/trips", TestContext.Current.CancellationToken))!;

        categories.ShouldBeEmpty();
        trips.ShouldBeEmpty();
    }
}
