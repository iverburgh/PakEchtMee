using System.Net;
using System.Net.Http.Json;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests.Access;

[IntegrationTest]
public sealed class ApiTokenValidationTests(JwtApiFixture fixture) : IClassFixture<JwtApiFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<HttpStatusCode> StatusForAsync(string? token, string path = "/api/trips")
        => (await fixture.ClientWithToken(token).GetAsync(path, CancellationToken)).StatusCode;

    [Fact]
    public async Task A_request_without_a_token_is_unauthorized()
        => (await StatusForAsync(null)).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task An_expired_token_is_unauthorized()
        => (await StatusForAsync(JwtApiFixture.Token("ivoverburgh@gmail.com", lifetime: TimeSpan.FromMinutes(-1)))).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task A_token_signed_with_another_key_is_unauthorized()
        => (await StatusForAsync(JwtApiFixture.Token("ivoverburgh@gmail.com", key: "another-key-that-is-also-long-enough-to-sign"))).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task A_token_for_another_audience_is_unauthorized()
        => (await StatusForAsync(JwtApiFixture.Token("ivoverburgh@gmail.com", audience: "someone-else"))).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task A_valid_token_for_a_member_is_accepted()
        => (await StatusForAsync(JwtApiFixture.Token("ivoverburgh@gmail.com"))).ShouldBe(HttpStatusCode.OK);

    [Fact]
    public async Task A_valid_token_for_a_stranger_is_forbidden()
        => (await StatusForAsync(JwtApiFixture.Token("vreemde@example.com"))).ShouldBe(HttpStatusCode.Forbidden);

    [Fact]
    public async Task The_health_check_needs_no_token()
        => (await StatusForAsync(null, "/health")).ShouldBe(HttpStatusCode.OK);

    [Fact]
    public async Task The_migration_makes_ivo_administrator_and_member_of_verburgh()
    {
        MeResponse me = (await fixture.ClientWithToken(JwtApiFixture.Token("IvoVerburgh@Gmail.com"))
            .GetFromJsonAsync<MeResponse>("/api/me", CancellationToken))!;

        me.Email.ShouldBe("ivoverburgh@gmail.com");
        me.IsAdministrator.ShouldBeTrue();
        me.Family!.Name.ShouldBe("Verburgh");
    }

    internal sealed record MeResponse(string Email, bool IsAdministrator, FamilyResponse? Family);

    internal sealed record FamilyResponse(Guid Id, string Name);
}
