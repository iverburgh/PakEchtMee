using Catalog.Infrastructure.Sql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Tests;
using Trips.Infrastructure.Sql;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerTestContainerFixture))]

namespace Web.Tests;

/// <summary>Hosts the API in memory against a real database on the shared container.</summary>
public sealed class ApiFixture(SqlServerTestContainerFixture server) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"api_{Guid.NewGuid():N}");

        using IServiceScope scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TripsDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // "Testing" keeps the startup migration and the OpenAPI document out of the way.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:pakechtmee", connectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");
    }
}
