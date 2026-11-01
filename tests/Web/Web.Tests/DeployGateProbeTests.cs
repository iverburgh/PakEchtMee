using Xunit;

namespace Web.Tests;

// Temporary: proves that a failing test stops the deployment. Reverted in the next commit.
public sealed class DeployGateProbeTests
{
    [Fact]
    public void Failing_test_blocks_deploy() => Assert.Fail("Intentional failure to verify the deploy gate.");
}
