using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.EventProcessors
{
    internal sealed class LogGroupingEventProcessor : EventProcessor
    {
        private readonly ILogger consoleLogger;

        public LogGroupingEventProcessor(ConsoleLogger logger)
        {
            consoleLogger = logger;
        }

        public override void OnStartValidationStage()
        {
            consoleLogger.WriteLine("##[group]ValidationStage");
        }

        public override void OnEndValidationStage()
        {
            consoleLogger.WriteLine("##[endgroup]");
        }

        public override void OnStartBuildStage(IReadOnlyList<BuildPartition> partitions)
        {
            consoleLogger.WriteLine("##[group]BuildStage");
        }

        public override void OnEndBuildStage()
        {
            consoleLogger.WriteLine("##[endgroup]");
        }

        public override void OnStartRunStage()
        {
            consoleLogger.WriteLine("##[group]RunStage");
        }

        public override void OnStartRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
        {
            consoleLogger.WriteLine($"##[group]RunBenchmarksInType: {type.GetDisplayName()}");
        }

        public override void OnEndRunBenchmarksInType(Type type, Summary summary)
        {
            consoleLogger.WriteLine("##[endgroup]");
        }

        public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
        {
            Console.WriteLine($"##[group]RunBenchmark: {benchmarkCase.DisplayInfo}");
        }

        public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
        {
            Console.WriteLine("##[endgroup]");
        }

        public override void OnEndRunStage()
        {
            Console.WriteLine("##[endgroup]");
        }
    }
}
