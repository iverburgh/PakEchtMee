using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Tests;
using Xunit;

namespace Web.Tests.Seeding;

/// <summary>Hosts the API in the Production environment on an empty database, so only startup could have filled it.</summary>
public sealed class ProductionApiFixture(SqlServerTestContainerFixture server) : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The migration makes this administrator a member of Verburgh, so it sees the general catalog.</summary>
    public const string VerburghAdministrator = "ivoverburgh@gmail.com";

    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"api_prod_{Guid.NewGuid():N}");

        // Touching the services builds the host, which runs the startup migration and seed.
        _ = Services;
    }

    /// <summary>A client that is signed in as <paramref name="email"/>; <c>null</c> gives an anonymous client.</summary>
    public HttpClient ClientFor(string? email)
    {
        HttpClient client = CreateClient();

        if (email is not null)
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, email);

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:pakechtmee", connectionString);
        builder.UseSetting("Auth:TokenKey", new string('k', 48));
        TestAuthenticationHandler.Use(builder);
    }
}
