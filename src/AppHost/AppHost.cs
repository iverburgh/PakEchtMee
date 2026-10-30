var builder = DistributedApplication.CreateBuilder(args);

// A pinned password (user secret "Parameters:sql-password") keeps the persistent container and its volume valid across restarts.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var database = sql.AddDatabase("pakechtmee");

// Supplied by the developer from Google Cloud Console; see docs/google-sign-in.md.
var googleClientId = builder.AddParameter("google-client-id");
var googleClientSecret = builder.AddParameter("google-client-secret", secret: true);

// Generated once and kept in user secrets, so sessions and API tokens survive restarts.
var nextAuthSecret = builder.AddParameter(
    "nextauth-secret", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
var apiTokenKey = builder.AddParameter(
    "api-token-key", new GenerateParameterDefault { MinLength = 48, Special = false }, secret: true, persist: true);

// The fixed, unproxied port also lets a Next.js dev server started outside Aspire reach the API.
var api = builder.AddProject<Projects.Web>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Auth__TokenKey", apiTokenKey)
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
// The port is fixed because Google only redirects to a pre-registered URI (http://localhost:3000/api/auth/callback/google).
builder.AddJavaScriptApp("frontend", "../web-client")
    .WithHttpEndpoint(port: 3000, env: "PORT", isProxied: false)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("api"))
    .WithEnvironment("NEXTAUTH_URL", "http://localhost:3000")
    .WithEnvironment("NEXTAUTH_SECRET", nextAuthSecret)
    .WithEnvironment("GOOGLE_CLIENT_ID", googleClientId)
    .WithEnvironment("GOOGLE_CLIENT_SECRET", googleClientSecret)
    .WithEnvironment("API_TOKEN_KEY", apiTokenKey)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
