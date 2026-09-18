using Catalog.Application;
using Catalog.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Trips.Application;
using Trips.Infrastructure.Sql;
using Web.Endpoints;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("pakechtmee")
    ?? throw new InvalidOperationException("Connection string 'pakechtmee' is not configured.");

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(connectionString);
builder.Services.AddTripsApplication();
builder.Services.AddTripsInfrastructure(connectionString);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// There is no authentication yet, so only the known frontend origins may call this API.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<TripsDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);

app.MapDefaultEndpoints();
app.MapCategoryEndpoints();
app.MapCatalogItemEndpoints();
app.MapTripEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can host the API through <c>WebApplicationFactory</c>.</summary>
public partial class Program;
