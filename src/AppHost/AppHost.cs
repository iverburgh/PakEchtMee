var builder = DistributedApplication.CreateBuilder(args);

// A pinned password (user secret "Parameters:sql-password") keeps the persistent container and its volume valid across restarts.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var database = sql.AddDatabase("pakechtmee");

// The fixed, unproxied port also lets a Next.js dev server started outside Aspire reach the API.
var api = builder.AddProject<Projects.Web>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpEndpoint(port: 5080, name: "api", isProxied: false)
    .WithHttpHealthCheck("/health")
    // The API has nothing on "/", so the dashboard links point at pages that actually respond.
    .WithUrlForEndpoint("api", url =>
    {
        url.Url = "/openapi/v1.json";
        url.DisplayText = "OpenAPI";
    })
    .WithUrlForEndpoint("http", url =>
    {
        url.Url = "/health";
        url.DisplayText = "Health";
    })
    .WithUrlForEndpoint("https", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly);

// AddNextJsApp is still experimental; the stable JavaScript app runs `npm run dev`, and Next.js reads PORT.
builder.AddJavaScriptApp("frontend", "../web-client")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("api"))
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
