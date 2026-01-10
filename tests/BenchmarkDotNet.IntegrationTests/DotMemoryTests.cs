using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DotMemoryTests : BenchmarkTestExecutor
    {
        public DotMemoryTests(ITestOutputHelper output) : base(output)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Output.WriteLine("Unhandled exception: " + args.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Output.WriteLine("Unobserved task exception: " + args.Exception);
            };
        }

        [Fact]
        public void DotMemorySmokeTest()
        {
            if (!OsDetector.IsWindows() && RuntimeInformation.IsMono)
            {
                Output.WriteLine("Skip Mono on non-Windows");
                return;
            }

            var config = new ManualConfig().AddJob(
                // Job.Dry.WithId("ExternalProcess")
                Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default).WithId("InProcess")
            );
            string snapshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "snapshots");
            if (Directory.Exists(snapshotDirectory))
                Directory.Delete(snapshotDirectory, true);

            foreach (var i in Enumerable.Range(1, 200))
            {
                CanExecute<Benchmarks>(config);
            }
        }

        [DotMemoryDiagnoser]
        public class Benchmarks
        {
            [Benchmark]
            public int Foo0()
            {
                var list = new List<object>();
                for (int i = 0; i < 1000; i++)
                    list.Add(new object());
                return list.Count;
            }
        }
    }
}