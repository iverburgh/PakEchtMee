using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests.Access;

[IntegrationTest]
public sealed class FamilyAdministrationEndpointsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient administrator = fixture.ClientFor(ApiFixture.Administrator);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..30];

    private async Task<FamilyResponse> CreateFamilyAsync(string name)
    {
        HttpResponseMessage response = await administrator.PostAsJsonAsync("/api/admin/families", new { Name = name }, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<FamilyResponse>(CancellationToken))!;
    }

    private Task<HttpResponseMessage> AddMemberAsync(Guid familyId, string email)
        => administrator.PostAsJsonAsync($"/api/admin/families/{familyId}/members", new { Email = email }, CancellationToken);

    [Fact]
    public async Task The_administrator_creates_renames_and_lists_families()
    {
        FamilyResponse created = await CreateFamilyAsync(Unique("Jansen"));

        HttpResponseMessage renamed = await administrator.PutAsJsonAsync(
            $"/api/admin/families/{created.Id}", new { Name = created.Name + " II" }, CancellationToken);
        renamed.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<FamilyResponse> families = (await administrator.GetFromJsonAsync<List<FamilyResponse>>("/api/admin/families", CancellationToken))!;
        families.ShouldContain(family => family.Id == created.Id && family.Name == created.Name + " II");
        families.ShouldContain(family => family.Id == fixture.FamilyA && family.Members.Contains(ApiFixture.MemberOfA));
    }

    [Fact]
    public async Task A_duplicate_family_name_is_rejected()
    {
        string name = Unique("Jansen");
        await CreateFamilyAsync(name);

        HttpResponseMessage response = await administrator.PostAsJsonAsync(
            "/api/admin/families", new { Name = name.ToUpperInvariant() }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_added_member_gets_access_and_a_removed_member_loses_it()
    {
        FamilyResponse family = await CreateFamilyAsync(Unique("Jansen"));
        string email = $"{Guid.NewGuid():N}@example.com";

        (await AddMemberAsync(family.Id, email)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await fixture.ClientFor(email).GetAsync("/api/me", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage removed = await administrator.DeleteAsync($"/api/admin/families/{family.Id}/members/{email}", CancellationToken);
        removed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await fixture.ClientFor(email).GetAsync("/api/me", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_member_of_another_family_is_rejected_with_its_name()
    {
        FamilyResponse family = await CreateFamilyAsync(Unique("Jansen"));

        HttpResponseMessage response = await AddMemberAsync(family.Id, ApiFixture.MemberOfB);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain("Familie B");
    }

    [Fact]
    public async Task An_invalid_address_is_rejected()
    {
        FamilyResponse family = await CreateFamilyAsync(Unique("Jansen"));

        (await AddMemberAsync(family.Id, "oma.jansen")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_member_cannot_manage_families()
    {
        HttpClient member = fixture.ClientFor(ApiFixture.MemberOfA);

        (await member.GetAsync("/api/admin/families", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await member.PostAsJsonAsync($"/api/admin/families/{fixture.FamilyA}/members", new { Email = "vriend@example.com" }, CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    internal sealed record FamilyResponse(Guid Id, string Name, IReadOnlyList<string> Members);
}
