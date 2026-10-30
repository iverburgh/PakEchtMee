using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shared.Tests;
using Xunit;

namespace Web.Tests.Access;

/// <summary>
/// Hosts the API with the real JWT bearer validation, in Development so the health endpoints exist and the startup
/// migration creates Verburgh with its first member.
/// </summary>
public sealed class JwtApiFixture(SqlServerTestContainerFixture server) : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TokenKey = "test-key-for-the-api-token-of-at-least-32-chars";

    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"api_jwt_{Guid.NewGuid():N}");
        _ = Services;
    }

    /// <summary>Signs a token the way the Next.js server does; every part can be varied to prove it is checked.</summary>
    public static string Token(
        string email,
        TimeSpan? lifetime = null,
        string key = TokenKey,
        string issuer = "pakechtmee-web",
        string audience = "pakechtmee-api")
    {
        DateTime now = DateTime.UtcNow;
        DateTime expires = now + (lifetime ?? TimeSpan.FromMinutes(5));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", email)]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires < now ? expires.AddMinutes(-5) : now,
            NotBefore = expires < now ? expires.AddMinutes(-5) : now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
    }

    public HttpClient ClientWithToken(string? token)
    {
        HttpClient client = CreateClient();

        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:pakechtmee", connectionString);
        builder.UseSetting("Auth:TokenKey", TokenKey);
    }
}
