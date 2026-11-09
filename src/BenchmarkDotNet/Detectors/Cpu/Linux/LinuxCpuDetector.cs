using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Helpers;
using Perfolizer.Models;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Detectors.Cpu.Linux;

/// <summary>
/// CPU information from output of the `cat /proc/cpuinfo` and `lscpu` command.
/// When the process is elevated, the CPU name and frequencies are overwritten that are taken from SMBIOS information.
/// </summary>
internal class LinuxCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsLinux();

    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        // lscpu output respects the system locale, so we should force language invariant environment for correct parsing
        var languageInvariantEnvironment = new Dictionary<string, string>
        {
            ["LC_ALL"] = "C",
            ["LANG"] = "C",
            ["LANGUAGE"] = "C"
        };

        string? cpuInfo = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo") ?? string.Empty;
        string? lscpu = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu\"", environmentVariables: languageInvariantEnvironment) ?? string.Empty;

        var result = LinuxCpuInfoParser.Parse(cpuInfo, lscpu);

        // Try to overwrite ProcessorName/Frequencies information by using SMBIOS data
        if (TryGetSmbiosCpuInfo(out var smbiosCpuInfo))
            ApplySmbiosNameAndFrequency(result, smbiosCpuInfo);

        return result;
    }

    private static bool TryGetSmbiosCpuInfo([NotNullWhen(true)] out CpuInfo? cpuInfo)
    {
        if (!SmbiosDataAccessor.TryGetSmbiosData(out _, out var smbiosData))
        {
            cpuInfo = null;
            return false;
        }

        var processors = SmbiosDataParser.ParseProcessorInfoTable(smbiosData);
        cpuInfo = SmbiosCpuDetector.FromProcessorInfo(processors);

        return cpuInfo != null;
    }

    internal static CpuInfo ApplySmbiosNameAndFrequency(CpuInfo cpuInfo, CpuInfo smbiosCpuInfo)
    {
        if (smbiosCpuInfo.ProcessorName is not null)
            cpuInfo.ProcessorName = smbiosCpuInfo.ProcessorName;
        if (smbiosCpuInfo.MaxFrequencyHz is not null)
            cpuInfo.MaxFrequencyHz = smbiosCpuInfo.MaxFrequencyHz;
        if (smbiosCpuInfo.NominalFrequencyHz is not null)
            cpuInfo.NominalFrequencyHz = smbiosCpuInfo.NominalFrequencyHz;

        return cpuInfo;
    }
}
