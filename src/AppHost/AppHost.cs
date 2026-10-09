var builder = DistributedApplication.CreateBuilder(args);

// A pinned password (user secret "Parameters:sql-password") keeps the persistent container and its volume valid across restarts.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var database = sql.AddDatabase("pakechtmee");

// A fixed, unproxied port so the Next.js dev server has a stable API address to talk to.
builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpEndpoint(port: 5080, name: "api", isProxied: false)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
