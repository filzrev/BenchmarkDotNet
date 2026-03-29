using AwesomeAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;

namespace BenchmarkDotNet.IntegrationTests;

/// <summary>
/// In order to run WasmTests locally, the following prerequisites are required:
/// * Install wasm-tools workload: `BenchmarkDotNet/build.cmd install-wasm-tools`
/// * Install npm
/// * Install v8: `npm install jsvu -g && jsvu --os=default --engines=v8`
/// * Add `$HOME/.jsvu/bin` to PATH
/// * Run tests using .NET SDK from `BenchmarkDotNet/.dotnet/`
/// </summary>
public class WasmTests : BenchmarkTestExecutor
{
    [Test]
    [NonWindowsArmAndNonLinuxArm]
    [TUnit.Core.Arguments(MonoAotCompilerMode.mini)]
    [TUnit.Core.Arguments(MonoAotCompilerMode.wasm)]
    public void WasmIsSupported(MonoAotCompilerMode aotCompilerMode)
    {
        CanExecute<WasmBenchmark>(GetConfig(aotCompilerMode).WithBuildTimeout(TimeSpan.FromMinutes(15)));
    }

    [Test]
    [NonWindowsArmAndNonLinuxArm]
    [TUnit.Core.Arguments(MonoAotCompilerMode.mini)]
    [TUnit.Core.Arguments(MonoAotCompilerMode.wasm)]
    public async Task WasmSupportsInProcessDiagnosers(MonoAotCompilerMode aotCompilerMode)
    {
        try
        {
            var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
            var config = GetConfig(aotCompilerMode).AddDiagnoser(diagnoser);

            CanExecute<WasmBenchmark>(config);

            diagnoser.Results.Values.Should().BeEquivalentTo([diagnoser.ExpectedResult]);
            BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result).Should().BeEquivalentTo([diagnoser.ExpectedResult]);
        }
        finally
        {
            BaseMockInProcessDiagnoser.s_completedResults.Clear();
        }
    }

    [Test]
    [NonWindowsArmAndNonLinuxArm]
    public async Task WasmSupportsCustomMainJs()
    {
        var summary = CanExecute<WasmBenchmark>(GetConfig(MonoAotCompilerMode.mini, true, true));

        var artefactsPaths = summary.Reports.Single().GenerateResult.ArtifactsPaths;
        File.ReadAllText(artefactsPaths.ExecutablePath).Should().Contain("custom-template-identifier");

        Directory.Delete(Path.GetDirectoryName(artefactsPaths.ProjectFilePath)!, true);
    }

    [Test]
    [NonWindowsArmAndNonLinuxArm]
    public void WasmSupportsNode()
    {
        CanExecute<WasmBenchmark>(GetConfig(MonoAotCompilerMode.mini, javaScriptEngine: "node"));
    }

    private ManualConfig GetConfig(MonoAotCompilerMode aotCompilerMode, bool useMainJsTemplate = false, bool keepBenchmarkFiles = false, string javaScriptEngine = "v8")
    {
        var dotnetVersion = "net8.0";
        var logger = new OutputLogger(Output);
        var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: aotCompilerMode);

        var mainJsTemplate = useMainJsTemplate ? new FileInfo(Path.Combine("wwwroot", "custom-main.mjs")) : null;

        return ManualConfig.CreateEmpty()
            .AddLogger(logger)
            .AddJob(Job.Dry
                .WithRuntime(new WasmRuntime(dotnetVersion, RuntimeMoniker.WasmNet80, "wasm", aotCompilerMode == MonoAotCompilerMode.wasm, javaScriptEngine, mainJsTemplate: mainJsTemplate))
                .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
            .WithBuildTimeout(TimeSpan.FromSeconds(900))
            .WithOption(ConfigOptions.KeepBenchmarkFiles, keepBenchmarkFiles)
            .WithOption(ConfigOptions.LogBuildOutput, true)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);
    }

    public class WasmBenchmark
    {
        [Benchmark]
        public void Check()
        {
            if (!RuntimeInformation.IsWasm)
            {
                throw new Exception("Incorrect runtime detection");
            }
        }
    }
}
