using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Tests;

/// <summary>
/// Stands in for the JWT bearer scheme: the e-mail address in <see cref="HeaderName"/> becomes the signed-in account.
/// Without the header the request is anonymous, so the API's own 401 handling is still exercised.
/// </summary>
internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var email) || string.IsNullOrWhiteSpace(email))
            return Task.FromResult(AuthenticateResult.NoResult());

        ClaimsIdentity identity = new([new Claim("sub", email.ToString())], SchemeName, "sub", null);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    /// <summary>Makes the test scheme the default for authenticating and challenging.</summary>
    public static void Use(IWebHostBuilder builder)
        => builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(SchemeName, null);
            services.PostConfigure<AuthenticationOptions>(authentication =>
            {
                authentication.DefaultScheme = SchemeName;
                authentication.DefaultAuthenticateScheme = SchemeName;
                authentication.DefaultChallengeScheme = SchemeName;
            });
        });
}
