using Catalog.Application;
using Catalog.Infrastructure.Sql;
using Families.Application;
using Families.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Trips.Application;
using Trips.Infrastructure.Sql;
using Web.Access;
using Web.Endpoints;
using Web.Seeding;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("pakechtmee")
    ?? throw new InvalidOperationException("Connection string 'pakechtmee' is not configured.");

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(connectionString);
builder.Services.AddTripsApplication();
builder.Services.AddTripsInfrastructure(connectionString);
builder.Services.AddFamiliesApplication();
builder.Services.AddFamiliesInfrastructure(connectionString);

builder.Services.AddAccessControl();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// The browser never calls this API (Next.js does, server-side); CORS stays as a second fence for known origins only.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<FamiliesDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<TripsDbContext>().Database.MigrateAsync();
    await ActivatorUtilities.CreateInstance<DevelopmentSeeder>(scope.ServiceProvider).SeedAsync(CancellationToken.None);
}

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapMeEndpoints();
app.MapCategoryEndpoints().RequireKnownUser();
app.MapCatalogItemEndpoints().RequireKnownUser();
app.MapTripEndpoints().RequireMember();
app.MapFamilyAdministrationEndpoints().RequireAdministrator();

await app.RunAsync();

/// <summary>Exposed so the integration tests can host the API through <c>WebApplicationFactory</c>.</summary>
public partial class Program;
