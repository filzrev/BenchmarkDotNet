using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Samples.Sandbox;

internal class Program
{
    public static int Main(string[] args)
    {
        var logger = ConsoleLogger.Default;
#if DEBUG
        logger.WriteLineWarning("Benchmark is executed with DEBUG configuration.");
        logger.WriteLine();
#endif

        if (args.Length != 0)
        {
            logger.WriteLine($"Start benchmarks with args: {string.Join(" ", args)}");
            logger.WriteLine();
        }

        // Get config
        var config = GetConfig(ref args);

        // Run benchmarks
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                                         .Run(args, config)
                                         .ToArray();

        if (summaries.HasError())
        {
            logger.WriteLine();
            logger.WriteLineWarning("Failed to running benchmarks.");
            return 1;
        }

        return 0;
    }

    private static IConfig GetConfig(ref string[] args)
    {
        // TODO: Return config based on commandline arguments.

        // Use config that based on https://github.com/dotnet/performance/blob/main/src/harness/BenchmarkDotNet.Extensions/RecommendedConfig.cs
        var config = ManualConfig.CreateEmpty()
                                 .WithBuildTimeout(TimeSpan.FromMinutes(10)) // Default: 120 seconds
                                 .AddLogger(ConsoleLogger.Default) // log output to console
                                 .AddValidator(DefaultConfig.Instance.GetValidators().ToArray())
                                 .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
                                 .AddExporter(MarkdownExporter.GitHub)
                                 .AddColumnProvider(DefaultColumnProviders.Instance)
                                 .WithArtifactsPath(DefaultConfig.Instance.ArtifactsPath)
                                 .AddDiagnoser(MemoryDiagnoser.Default)
                                 .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // Default: 20

        var baseJob = Job.Default
                         .WithWarmupCount(1)
                         .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slighlty too much for us
                         .DontEnforcePowerPlan();

#if DEBUG
        var job = baseJob.WithToolchain(InProcessEmitToolchain.Default)
                         .WithStrategy(RunStrategy.Monitoring)
                         .WithId($"InProcessMonitoring({RuntimeInformation.FrameworkDescription})");
        config.AddJob(job)
              .WithOptions(ConfigOptions.DisableOptimizationsValidator);
#else
        var job = baseJob.WithToolchain(Constants.DefaultToolchain)
                         .WithId($"Default({Constants.DefaultToolchain.Name})");
        config.AddJob(job);
#endif

        // Return immutable config.
        return config.CreateImmutableConfig();
    }
}
