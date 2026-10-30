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
    }
}
