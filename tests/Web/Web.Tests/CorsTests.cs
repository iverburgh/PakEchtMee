using Shared.Tests;
using Shouldly;
using Xunit;

namespace Web.Tests;

[IntegrationTest]
public sealed class CorsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static HttpRequestMessage Preflight(string origin)
    {
        HttpRequestMessage request = new(HttpMethod.Options, "/api/categories");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        return request;
    }

    [Fact]
    public async Task The_configured_frontend_origin_is_allowed()
    {
        HttpResponseMessage response = await fixture.CreateClient()
            .SendAsync(Preflight("http://localhost:3000"), TestContext.Current.CancellationToken);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? allowed).ShouldBeTrue();
        allowed!.ShouldContain("http://localhost:3000");
    }

    [Fact]
    public async Task An_unlisted_origin_gets_no_cors_approval()
    {
        HttpResponseMessage response = await fixture.CreateClient()
            .SendAsync(Preflight("https://evil.example"), TestContext.Current.CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }
}
