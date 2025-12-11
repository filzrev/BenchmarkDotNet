using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
#if !DEBUG
using Xunit;
#endif
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomBuildConfigurationTests : BenchmarkTestExecutor
    {
        public CustomBuildConfigurationTests(ITestOutputHelper output) : base(output)
        {
        }

        [FactEnvSpecific("Flaky, see https://github.com/dotnet/BenchmarkDotNet/issues/2376", EnvRequirement.NonFullFramework)]
        public void UserCanSpecifyCustomBuildConfiguration()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("DOTNET_ROOT:" + Environment.GetEnvironmentVariable("DOTNET_ROOT"));
            Console.WriteLine("DOTNET_ROOT_ARM64 :" + Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64 "));
            Console.WriteLine("DOTNET_ROOT_X64 :" + Environment.GetEnvironmentVariable("DOTNET_ROOT_X64 "));
            Console.WriteLine("DOTNET_ROOT_X86 :" + Environment.GetEnvironmentVariable("DOTNET_ROOT_X86 "));
            Console.WriteLine("BaseDirectory:" + AppContext.BaseDirectory);
            Console.WriteLine("========================================");

            var jobWithCustomConfiguration = Job.Dry.WithCustomBuildConfiguration("CUSTOM")
                                                    .WithRuntime(RuntimeInformation.GetCurrentRuntime());

            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);
            config = ((ManualConfig)config).WithBuildTimeout(TimeSpan.FromSeconds(240));

            var report = CanExecute<CustomBuildConfiguration>(config);

#if !DEBUG
            Assert.NotEqual(RuntimeInformation.DebugConfigurationName, report.HostEnvironmentInfo.Configuration);
            Assert.DoesNotContain(report.AllRuntimes, RuntimeInformation.DebugConfigurationName);
#endif
        }

        public class CustomBuildConfiguration
        {
            [Benchmark]
            public void Benchmark()
            {
                if (Assembly.GetEntryAssembly().IsJitOptimizationDisabled().IsTrue())
                {
                    throw new InvalidOperationException("Auto-generated project has not enabled optimizations!");
                }
                if (typeof(CustomBuildConfiguration).Assembly.IsJitOptimizationDisabled().IsTrue())
                {
                    throw new InvalidOperationException("Project that defines benchmarks has not enabled optimizations!");
                }
                if (RuntimeInformation.GetConfiguration() == RuntimeInformation.DebugConfigurationName)
                {
                    throw new InvalidOperationException($"Configuration rezognized as {RuntimeInformation.DebugConfigurationName}!");
                }

#if !CUSTOM
                throw new InvalidOperationException("Should never happen");
#endif
            }
        }
    }
}