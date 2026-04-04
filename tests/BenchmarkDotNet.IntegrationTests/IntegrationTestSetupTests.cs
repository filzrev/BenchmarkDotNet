using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.IntegrationTests;

[LogOnStart]
public class IntegrationTestSetupTests
{
    [Fact]
    public void IntegrationTestsAreDetected() => Assert.True(XUnitHelper.IsIntegrationTest.Value);
}