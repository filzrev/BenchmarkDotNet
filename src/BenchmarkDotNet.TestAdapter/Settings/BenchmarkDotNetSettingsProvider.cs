using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace BenchmarkDotNet.TestAdapter;

/// <summary>
/// ISettingsProvider implementation that load settings from `.runsettings` configuration file.
/// </summary>
[SettingsName(Name)]
public class BenchmarkDotNetSettingsProvider : ISettingsProvider
{
    private const string Name = "BenchmarkDotNet";

    /// <summary>
    /// This method is automatically invoked when runsetting file contains BenchmarkDotNet section
    /// </summary>
    public void Load(XmlReader reader)
    {
        var document = XDocument.Load(reader);

        var settings = document.Root;
        var argumentsValue = settings.Elements("Arguments").FirstOrDefault()?.Value ?? "";
         
        if (argumentsValue.IsBlank())
            return;

        var arguments = argumentsValue.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        var logger = new AccumulationLogger();
        var (isSuccess, parsedConfig, options) = ConfigParser.Parse(arguments, logger);
        if (!isSuccess)
        {
            EqtTrace.Error("Failed to parse arguments. " + logger.GetLog());
            return;
        }

        // Create custom default config.
        var config = ManualConfig.Union(DefaultConfig.Default, parsedConfig!);

        // Set custom DefaultConfig instance.
        DefaultConfig.SetDefault(config);
    }
}
