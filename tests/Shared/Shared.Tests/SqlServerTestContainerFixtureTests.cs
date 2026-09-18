using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

[assembly: AssemblyFixture(typeof(Shared.Tests.SqlServerTestContainerFixture))]

namespace Shared.Tests;

[IntegrationTest]
public sealed class SqlServerTestContainerFixtureTests(SqlServerTestContainerFixture fixture)
{
    [Fact]
    public async Task The_container_accepts_a_connection()
    {
        await using SqlConnection connection = new(fixture.ConnectionString);

        await connection.OpenAsync(TestContext.Current.CancellationToken);

        connection.State.ShouldBe(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task A_fresh_database_can_be_created_per_test_class()
    {
        string connectionString = await fixture.CreateDatabaseAsync("SmokeTest");

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        connection.Database.ShouldBe("SmokeTest");
    }
}
