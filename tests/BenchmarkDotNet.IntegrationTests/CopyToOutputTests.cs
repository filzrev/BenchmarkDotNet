#if NETFRAMEWORK
using BenchmarkDotNet.IntegrationTests.CustomPaths;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [LogOnStart]
    public class CopyToOutputTests : BenchmarkTestExecutor
    {
        public CopyToOutputTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CopyToOutputIsSupported() => CanExecute<BenchmarksThatUsesFileFromOutput>();
    }
}
#endif