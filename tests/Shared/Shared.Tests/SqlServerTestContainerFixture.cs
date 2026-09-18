using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace Shared.Tests;

/// <summary>
/// One SQL Server container per test assembly. Every test class asks for its own database on that server,
/// so classes stay isolated without paying for a container each.
/// </summary>
public sealed class SqlServerTestContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    /// <summary>Creates an empty database and returns a connection string pointing at it.</summary>
    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE [{databaseName.Replace("]", "]]", StringComparison.Ordinal)}]')";
        command.Parameters.AddWithValue("@name", databaseName);
        await command.ExecuteNonQueryAsync();

        return new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = databaseName }.ConnectionString;
    }
}
