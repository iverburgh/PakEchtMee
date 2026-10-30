using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Tests;
using Xunit;

namespace Web.Tests.Seeding;

/// <summary>Hosts the API in the Development environment, so startup migrates and seeds an empty database by itself.</summary>
public sealed class DevelopmentApiFixture(SqlServerTestContainerFixture server) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"api_dev_{Guid.NewGuid():N}");

        // Touching the services builds the host, which runs the startup migration and seed.
        _ = Services;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:pakechtmee", connectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");
        TestAuthenticationHandler.Use(builder);
    }

    /// <summary>Creates a fresh family through the administrator endpoints and returns a client for its only member.</summary>
    public async Task<HttpClient> ClientForNewFamilyAsync()
    {
        HttpClient administrator = ClientForVerburgh();
        HttpResponseMessage created = await administrator.PostAsJsonAsync(
            "/api/admin/families", new { Name = $"Familie {Guid.NewGuid():N}"[..20] }, TestContext.Current.CancellationToken);
        Guid familyId = (await created.Content.ReadFromJsonAsync<TripEndpointsTests.IdOnly>(TestContext.Current.CancellationToken))!.Id;
        string email = $"{Guid.NewGuid():N}@example.com";
        await administrator.PostAsJsonAsync($"/api/admin/families/{familyId}/members", new { Email = email }, TestContext.Current.CancellationToken);

        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, email);

        return client;
    }

    /// <summary>A client signed in as the administrator, who the migration makes a member of Verburgh.</summary>
    public HttpClient ClientForVerburgh()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, "ivoverburgh@gmail.com");

        return client;
    }
}
