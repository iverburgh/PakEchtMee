using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;
using static Web.Tests.Access.ApiTokenValidationTests;

namespace Web.Tests.Access;

[IntegrationTest]
public sealed class AccessEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> GetMeAsync(string? email) => fixture.ClientFor(email).GetAsync("/api/me", CancellationToken);

    [Fact]
    public async Task A_member_sees_their_family()
    {
        MeResponse me = (await (await GetMeAsync(ApiFixture.MemberOfA)).Content.ReadFromJsonAsync<MeResponse>(CancellationToken))!;

        me.Email.ShouldBe(ApiFixture.MemberOfA);
        me.IsAdministrator.ShouldBeFalse();
        me.Family.ShouldBe(new FamilyResponse(fixture.FamilyA, "Familie A"));
    }

    [Fact]
    public async Task An_administrator_without_a_family_is_known()
    {
        MeResponse me = (await (await GetMeAsync(ApiFixture.Administrator)).Content.ReadFromJsonAsync<MeResponse>(CancellationToken))!;

        me.IsAdministrator.ShouldBeTrue();
        me.Family.ShouldBeNull();
    }

    [Fact]
    public async Task Case_does_not_matter_for_the_address()
    {
        MeResponse me = (await (await GetMeAsync("Lid.A@Example.COM")).Content.ReadFromJsonAsync<MeResponse>(CancellationToken))!;

        me.Email.ShouldBe(ApiFixture.MemberOfA);
        me.Family!.Id.ShouldBe(fixture.FamilyA);
    }

    [Fact]
    public async Task A_stranger_is_forbidden()
        => (await GetMeAsync(ApiFixture.Stranger)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    [Fact]
    public async Task Anonymous_is_unauthorized()
        => (await GetMeAsync(null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task A_stranger_cannot_read_the_catalog()
        => (await fixture.ClientFor(ApiFixture.Stranger).GetAsync("/api/categories", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    [Fact]
    public async Task An_administrator_without_a_family_cannot_use_trips()
        => (await fixture.ClientFor(ApiFixture.Administrator).GetAsync("/api/trips", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
}
