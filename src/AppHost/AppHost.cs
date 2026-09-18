var builder = DistributedApplication.CreateBuilder(args);

// No data volume: Aspire generates a new sa password whenever the container is recreated, and a
// kept volume would still hold the old one. The container itself is persistent, so data survives restarts.
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sql.AddDatabase("pakechtmee");

// A fixed, unproxied port so the Next.js dev server has a stable API address to talk to.
builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpEndpoint(port: 5080, name: "api", isProxied: false)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
