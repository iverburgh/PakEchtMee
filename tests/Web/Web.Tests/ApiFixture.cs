using Catalog.Infrastructure.Sql;
using Families.Domain;
using Families.Infrastructure.Sql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Tests;
using Trips.Infrastructure.Sql;
using Xunit;

[assembly: AssemblyFixture(typeof(SqlServerTestContainerFixture))]

namespace Web.Tests;

/// <summary>
/// Hosts the API in memory against a real database on the shared container, with two families and an administrator
/// without a family. Requests authenticate through <see cref="TestAuthenticationHandler"/>.
/// </summary>
public sealed class ApiFixture(SqlServerTestContainerFixture server) : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string MemberOfA = "lid.a@example.com";
    public const string MemberOfB = "lid.b@example.com";
    public const string Administrator = "beheerder@example.com";
    public const string Stranger = "vreemde@example.com";

    private string connectionString = string.Empty;

    public Guid FamilyA { get; private set; }

    public Guid FamilyB { get; private set; }

    public async ValueTask InitializeAsync()
    {
        connectionString = await server.CreateDatabaseAsync($"api_{Guid.NewGuid():N}");

        using IServiceScope scope = Services.CreateScope();
        FamiliesDbContext families = scope.ServiceProvider.GetRequiredService<FamiliesDbContext>();
        await families.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TripsDbContext>().Database.MigrateAsync();

        Family a = Family.Create(FamilyName.Create("Familie A"));
        a.AddMember(EmailAddress.Create(MemberOfA));
        Family b = Family.Create(FamilyName.Create("Familie B"));
        b.AddMember(EmailAddress.Create(MemberOfB));
        families.Families.AddRange(a, b);
        await families.SaveChangesAsync();

        FamilyA = a.Id;
        FamilyB = b.Id;
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

        // "Testing" keeps the startup migration and the OpenAPI document out of the way.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:pakechtmee", connectionString);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");
        builder.UseSetting("Access:Administrators:1", Administrator);
        TestAuthenticationHandler.Use(builder);
    }
}
