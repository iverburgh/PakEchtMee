using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests;

/// <summary>The family-composition scenarios: members maintain their own family's size.</summary>
[IntegrationTest]
public sealed class FamilyCompositionEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient memberB = fixture.ClientFor(ApiFixture.MemberOfB);
    private readonly HttpClient administrator = fixture.ClientFor(ApiFixture.Administrator);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    internal sealed record CompositionResponse(int Adults, int Children);

    [Fact]
    public async Task A_member_reads_and_changes_the_family_composition()
    {
        CompositionResponse? before = await memberB.GetFromJsonAsync<CompositionResponse>("/api/family/composition", CancellationToken);
        before.ShouldBe(new CompositionResponse(2, 0));

        HttpResponseMessage changed = await memberB.PutAsJsonAsync("/api/family/composition", new { Adults = 2, Children = 3 }, CancellationToken);

        changed.StatusCode.ShouldBe(HttpStatusCode.OK);
        CompositionResponse? after = await memberB.GetFromJsonAsync<CompositionResponse>("/api/family/composition", CancellationToken);
        after.ShouldBe(new CompositionResponse(2, 3));
    }

    [Fact]
    public async Task An_empty_composition_is_rejected()
    {
        HttpResponseMessage response = await memberB.PutAsJsonAsync("/api/family/composition", new { Adults = 0, Children = 0 }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_administrator_without_a_family_is_forbidden()
    {
        HttpResponseMessage read = await administrator.GetAsync("/api/family/composition", CancellationToken);
        HttpResponseMessage change = await administrator.PutAsJsonAsync("/api/family/composition", new { Adults = 2, Children = 1 }, CancellationToken);

        read.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        change.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
