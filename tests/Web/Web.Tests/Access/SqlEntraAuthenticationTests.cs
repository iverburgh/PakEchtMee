using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Web.Tests.Access;

/// <summary>
/// Production signs in to Azure SQL with a managed identity. SqlClient 7 only offers that through the separate
/// Microsoft.Data.SqlClient.Extensions.Azure package; without it the API crashes at the first connection.
/// </summary>
public sealed class SqlEntraAuthenticationTests
{
    [Fact]
    public void Managed_identity_sign_in_is_available_to_SqlClient()
    {
        // Referencing Program pulls the Web assembly, and with it the extension, into the test like production.
        _ = typeof(Program);

        SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity).ShouldNotBeNull();
    }
}
