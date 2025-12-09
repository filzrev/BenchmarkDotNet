using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.IntegrationTests;

internal static class ManualConfigExtensions
{
    /// <summary>
    /// Helper extensiong method to suppress validation errors that are raised by ConfigValidator/JitOptimizationsValidator.
    /// </summary>
    public static ManualConfig SuppressValidatorMessages(this ManualConfig config)
    {
        if (!config.GetLoggers().Any())
            config.AddLogger(NullLogger.Instance);

        if (!config.GetExporters().Any())
            config.AddExporter(NullExporter.Instance);

        if (!config.GetColumnProviders().Any())
            config.AddColumnProvider(NullColumnProvider.Instance);

        config.WithOptions(ConfigOptions.DisableOptimizationsValidator);

        return config;
    }

    private class NullExporter : ExporterBase
    {
        public static IExporter Instance = new NullExporter();

        public override void ExportToLog(Summary summary, ILogger logger) { }
    }

    private class NullColumnProvider : IColumnProvider
    {
        public static IColumnProvider Instance = new NullColumnProvider();

        public IEnumerable<IColumn> GetColumns(Summary summary) => [];
    }
}
