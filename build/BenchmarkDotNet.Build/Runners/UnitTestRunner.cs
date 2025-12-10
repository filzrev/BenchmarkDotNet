using BenchmarkDotNet.Build.Helpers;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Build.Runners;

public class UnitTestRunner(BuildContext context)
{
    private FilePath UnitTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Tests.csproj");

    private FilePath ExporterTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Exporters.Plotting.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Exporters.Plotting.Tests.csproj");

    private FilePath AnalyzerTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.Analyzers.Tests")
        .CombineWithFilePath("BenchmarkDotNet.Analyzers.Tests.csproj");

    private FilePath IntegrationTestsProjectFile { get; } = context.RootDirectory
        .Combine("tests")
        .Combine("BenchmarkDotNet.IntegrationTests")
        .CombineWithFilePath("BenchmarkDotNet.IntegrationTests.csproj");

    private DirectoryPath TestOutputDirectory { get; } = context.RootDirectory
        .Combine("TestResults");

    private DotNetTestSettings GetTestSettingsParameters(FilePath logFile, string tfm)
    {
        var settings = new DotNetTestSettings
        {
            Configuration = context.BuildConfiguration,
            Framework = tfm,
            NoBuild = true,
            NoRestore = true,
            Loggers = new[] { "trx", $"trx;LogFileName={logFile.FullPath}", "console;verbosity=detailed" },
            EnvironmentVariables =
            {
                ["Platform"] = "" // force the tool to not look for the .dll in platform-specific directory
            }
        };
        return settings;
    }

    private void RunTests(FilePath projectFile, string alias, string tfm)
    {
        FixEnvironmentVariables();

        var os = Utils.GetOs();
        var arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
        var trxFileName = $"{os}({arch})-{alias}-{tfm}.trx";
        var trxFile = TestOutputDirectory.CombineWithFilePath(trxFileName);
        var settings = GetTestSettingsParameters(trxFile, tfm);

        context.Information($"Run tests for {projectFile} ({tfm}), result file: '{trxFile}'");
        context.DotNetTest(projectFile.FullPath, settings);
    }

    private void RunUnitTests(string tfm)
    {
        RunTests(UnitTestsProjectFile, "unit", tfm);
        RunTests(ExporterTestsProjectFile, "exporters", tfm);
    }

    public void RunUnitTests()
    {
        string[] targetFrameworks = context.IsRunningOnWindows() ? ["net462", "net8.0"] : ["net8.0"];
        foreach (var targetFramework in targetFrameworks)
            RunUnitTests(targetFramework);
    }

    public void RunAnalyzerTests()
    {
        string[] targetFrameworks = context.IsRunningOnWindows() ? ["net462", "net8.0"] : ["net8.0"];
        foreach (var targetFramework in targetFrameworks)
            RunTests(AnalyzerTestsProjectFile, "analyzer", targetFramework);
    }

    public void RunInTests(string tfm) => RunTests(IntegrationTestsProjectFile, "integration", tfm);

    // Helper method to fix environment variables that are set by `build.sh`/`build.ps1`.
    private static void FixEnvironmentVariables()
    {
        // Update `PATH` environment variable.
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(path))
        {
            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            var parts = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            var result = string.Join(separator, parts.Where(x => x != dotnetRoot));
            Environment.SetEnvironmentVariable("PATH", result);
        }

        // Clear `DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX`/`DOTNET_ROOT` environment variable.
        Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", null);
        Environment.SetEnvironmentVariable("DOTNET_ROOT", null);
    }
}