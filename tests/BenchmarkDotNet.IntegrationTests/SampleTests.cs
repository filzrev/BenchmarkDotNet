using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SampleTests : BenchmarkTestExecutor
    {
        public SampleTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            Console.WriteLine($"CPU: {CpuDetector.Cpu!.ProcessorName!}");
            // lscpu output respects the system locale, so we should force language invariant environment for correct parsing
            var languageInvariantEnvironment = new Dictionary<string, string>
            {
                ["LC_ALL"] = "C",
                ["LANG"] = "C",
                ["LANGUAGE"] = "C"
            };

            string? cpuInfo = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo") ?? string.Empty;
            string? lscpu = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu\"", environmentVariables: languageInvariantEnvironment) ?? string.Empty;

            Console.WriteLine(cpuInfo);
            Console.WriteLine("---");
            Console.WriteLine(lscpu);
        }
    }
}
