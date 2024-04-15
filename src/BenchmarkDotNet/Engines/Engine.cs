﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    [UsedImplicitly]
    public class Engine : IEngine
    {
        public const int MinInvokeCount = 4;

        [PublicAPI] public IHost Host { get; }
        [PublicAPI] public Action<long> WorkloadAction { get; }
        [PublicAPI] public Action Dummy1Action { get; }
        [PublicAPI] public Action Dummy2Action { get; }
        [PublicAPI] public Action Dummy3Action { get; }
        [PublicAPI] public Action<long> OverheadAction { get; }
        [PublicAPI] public Job TargetJob { get; }
        [PublicAPI] public long OperationsPerInvoke { get; }
        [PublicAPI] public Action GlobalSetupAction { get; }
        [PublicAPI] public Action GlobalCleanupAction { get; }
        [PublicAPI] public Action IterationSetupAction { get; }
        [PublicAPI] public Action IterationCleanupAction { get; }
        [PublicAPI] public IResolver Resolver { get; }
        [PublicAPI] public CultureInfo CultureInfo { get; }
        [PublicAPI] public string BenchmarkName { get; }

        private IClock Clock { get; }
        private bool ForceGcCleanups { get; }
        private int UnrollFactor { get; }
        private RunStrategy Strategy { get; }
        private bool EvaluateOverhead { get; }
        private bool MemoryRandomization { get; }

        private readonly List<Measurement> jittingMeasurements = new (10);
        private readonly EnginePilotStage pilotStage;
        private readonly EngineWarmupStage warmupStage;
        private readonly EngineActualStage actualStage;
        private readonly bool includeExtraStats;
        private readonly Random random;

        internal Engine(
            IHost host,
            IResolver resolver,
            Action dummy1Action, Action dummy2Action, Action dummy3Action, Action<long> overheadAction, Action<long> workloadAction, Job targetJob,
            Action globalSetupAction, Action globalCleanupAction, Action iterationSetupAction, Action iterationCleanupAction, long operationsPerInvoke,
            bool includeExtraStats, string benchmarkName)
        {

            Host = host;
            OverheadAction = overheadAction;
            Dummy1Action = dummy1Action;
            Dummy2Action = dummy2Action;
            Dummy3Action = dummy3Action;
            WorkloadAction = workloadAction;
            TargetJob = targetJob;
            GlobalSetupAction = globalSetupAction;
            GlobalCleanupAction = globalCleanupAction;
            IterationSetupAction = iterationSetupAction;
            IterationCleanupAction = iterationCleanupAction;
            OperationsPerInvoke = operationsPerInvoke;
            this.includeExtraStats = includeExtraStats;
            BenchmarkName = benchmarkName;

            Resolver = resolver;

            Clock = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, Resolver);
            ForceGcCleanups = targetJob.ResolveValue(GcMode.ForceCharacteristic, Resolver);
            UnrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, Resolver);
            Strategy = targetJob.ResolveValue(RunMode.RunStrategyCharacteristic, Resolver);
            EvaluateOverhead = targetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, Resolver);
            MemoryRandomization = targetJob.ResolveValue(RunMode.MemoryRandomizationCharacteristic, Resolver);

            warmupStage = new EngineWarmupStage(this);
            pilotStage = new EnginePilotStage(this);
            actualStage = new EngineActualStage(this);

            random = new Random(12345); // we are using constant seed to try to get repeatable results
        }

        public void Dispose()
        {
            try
            {
                GlobalCleanupAction?.Invoke();
            }
            catch (Exception e)
            {
                Host.SendError("Exception during GlobalCleanup!");
                Host.SendError(e.Message);

                // we don't rethrow because this code is executed in a finally block
                // and it could possibly overwrite current exception #1045
            }
        }

        public RunResults Run()
        {
            var measurements = new List<Measurement>();
            measurements.AddRange(jittingMeasurements);

            long invokeCount = TargetJob.ResolveValue(RunMode.InvocationCountCharacteristic, Resolver, 1);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStart(BenchmarkName);

            if (Strategy != RunStrategy.ColdStart)
            {
                if (Strategy != RunStrategy.Monitoring)
                {
                    var pilotStageResult = pilotStage.Run();
                    invokeCount = pilotStageResult.PerfectInvocationCount;
                    measurements.AddRange(pilotStageResult.Measurements);

                    if (EvaluateOverhead)
                    {
                        measurements.AddRange(warmupStage.RunOverhead(invokeCount, UnrollFactor));
                        measurements.AddRange(actualStage.RunOverhead(invokeCount, UnrollFactor));
                    }
                }

                measurements.AddRange(warmupStage.RunWorkload(invokeCount, UnrollFactor, Strategy));
            }

            Host.BeforeMainRun();

            measurements.AddRange(actualStage.RunWorkload(invokeCount, UnrollFactor, forceSpecific: Strategy == RunStrategy.Monitoring));

            Host.AfterMainRun();

            (GcStats workGcHasDone, ThreadingStats threadingStats, double exceptionFrequency) = includeExtraStats
                ? GetExtraStats(new IterationData(IterationMode.Workload, IterationStage.Actual, 0, invokeCount, UnrollFactor))
                : (GcStats.Empty, ThreadingStats.Empty, 0);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.BenchmarkStop(BenchmarkName);

            var outlierMode = TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, Resolver);

            return new RunResults(measurements, outlierMode, workGcHasDone, threadingStats, exceptionFrequency);
        }

        public Measurement RunIteration(IterationData data)
        {
            // Initialization
            long invokeCount = data.InvokeCount;
            int unrollFactor = data.UnrollFactor;
            long totalOperations = invokeCount * OperationsPerInvoke;
            bool isOverhead = data.IterationMode == IterationMode.Overhead;
            bool randomizeMemory = !isOverhead && MemoryRandomization;
            var action = isOverhead ? OverheadAction : WorkloadAction;

            if (!isOverhead)
                IterationSetupAction();

            GcCollect();

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStart(data.IterationMode, data.IterationStage, totalOperations);

            var clockSpan = randomizeMemory
                ? MeasureWithRandomMemory(action, invokeCount / unrollFactor)
                : Measure(action, invokeCount / unrollFactor);

            if (EngineEventSource.Log.IsEnabled())
                EngineEventSource.Log.IterationStop(data.IterationMode, data.IterationStage, totalOperations);

            if (!isOverhead)
                IterationCleanupAction();

            if (randomizeMemory)
                RandomizeManagedHeapMemory();

            GcCollect();

            // Results
            var measurement = new Measurement(0, data.IterationMode, data.IterationStage, data.Index, totalOperations, clockSpan.GetNanoseconds());
            WriteLine(measurement.ToString());
            if (measurement.IterationStage == IterationStage.Jitting)
                jittingMeasurements.Add(measurement);

            return measurement;
        }

        // This is in a separate method, because stackalloc can affect code alignment,
        // resulting in unexpected measurements on some AMD cpus,
        // even if the stackalloc branch isn't executed. (#2366)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe ClockSpan MeasureWithRandomMemory(Action<long> action, long invokeCount)
        {
            byte* stackMemory = stackalloc byte[random.Next(32)];
            var clockSpan = Measure(action, invokeCount);
            Consume(stackMemory);
            return clockSpan;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void Consume(byte* _) { }

        private ClockSpan Measure(Action<long> action, long invokeCount)
        {
            var clock = Clock.Start();
            action(invokeCount);
            return clock.GetElapsed();
        }

        private (GcStats, ThreadingStats, double) GetExtraStats(IterationData data)
        {
            // Warm up the GetAllocatedBytes function before starting the actual measurement.
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(GcStats.ReadInitial());

            IterationSetupAction(); // we run iteration setup first, so even if it allocates, it is not included in the results

            var initialThreadingStats = ThreadingStats.ReadInitial(); // this method might allocate
            var exceptionsStats = new ExceptionsStats(); // allocates
            exceptionsStats.StartListening(); // this method might allocate

            // GC collect before measuring allocations, as we do not collect during the measurement.
            ForceGcCollect();
            var gcStats = MeasureWithGc(data.InvokeCount / data.UnrollFactor);

            exceptionsStats.Stop(); // this method might (de)allocate
            var finalThreadingStats = ThreadingStats.ReadFinal();

            IterationCleanupAction(); // we run iteration cleanup after collecting GC stats

            var totalOperationsCount = data.InvokeCount * OperationsPerInvoke;
            gcStats = gcStats.WithTotalOperations(totalOperationsCount);
            ThreadingStats threadingStats = (finalThreadingStats - initialThreadingStats).WithTotalOperations(data.InvokeCount * OperationsPerInvoke);

            return (gcStats, threadingStats, exceptionsStats.ExceptionsCount / (double)totalOperationsCount);
        }

        // Isolate the allocation measurement and skip tier0 jit to make sure we don't get any unexpected allocations.
        [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
        private GcStats MeasureWithGc(long invokeCount)
        {
            var initialGcStats = GcStats.ReadInitial();
            WorkloadAction(invokeCount);
            var finalGcStats = GcStats.ReadFinal();
            return finalGcStats - initialGcStats;
        }

        private void RandomizeManagedHeapMemory()
        {
            // invoke global cleanup before global setup
            GlobalCleanupAction?.Invoke();

            var gen0object = new byte[random.Next(32)];
            var lohObject = new byte[85 * 1024 + random.Next(32)];

            // we expect the key allocations to happen in global setup (not ctor)
            // so we call it while keeping the random-size objects alive
            GlobalSetupAction?.Invoke();

            GC.KeepAlive(gen0object);
            GC.KeepAlive(lohObject);

            // we don't enforce GC.Collects here as engine does it later anyway
        }

        private void GcCollect()
        {
            if (!ForceGcCleanups)
                return;

            ForceGcCollect();
        }

        internal static void ForceGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void WriteLine(string text) => Host.WriteLine(text);

        public void WriteLine() => Host.WriteLine();

        [UsedImplicitly]
        public static class Signals
        {
            public const string Acknowledgment = "Acknowledgment";

            private static readonly Dictionary<HostSignal, string> SignalsToMessages
                = new Dictionary<HostSignal, string>
                {
                    { HostSignal.BeforeAnythingElse, "// BeforeAnythingElse" },
                    { HostSignal.BeforeActualRun, "// BeforeActualRun" },
                    { HostSignal.AfterActualRun, "// AfterActualRun" },
                    { HostSignal.AfterAll, "// AfterAll" }
                };

            private static readonly Dictionary<string, HostSignal> MessagesToSignals
                = SignalsToMessages.ToDictionary(p => p.Value, p => p.Key);

            public static string ToMessage(HostSignal signal) => SignalsToMessages[signal];

            public static bool TryGetSignal(string message, out HostSignal signal)
                => MessagesToSignals.TryGetValue(message, out signal);
        }
    }
}