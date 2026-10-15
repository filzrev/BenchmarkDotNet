using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Extensions;
using Perfolizer.Horology;
using Perfolizer.Models;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

/// <summary>
/// CPU information from the SMBIOS Type 4 (Processor Information) structures.
/// Windows only. Does not require WMI/WMIC/PowerShell.
/// </summary>
internal class SmbiosCpuDetector : ICpuDetector
{
    public bool IsApplicable() => OsDetector.IsWindows7OrLater() && !Portability.RuntimeInformation.IsMono;

    [SupportedOSPlatform("windows6.1")]
    public CpuInfo? Detect()
    {
        if (!IsApplicable())
            return null;

        try
        {
            IReadOnlyList<SmbiosProcessorInfo> cpus = SmbiosParser.GetCpuInfo();
            return FromSmbios(cpus);
        }
        catch
        {
            return null;
        }
    }

    internal static CpuInfo? FromSmbios(IReadOnlyList<SmbiosProcessorInfo> cpus)
    {
        if (cpus.Count == 0)
            return null;

        var processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        double maxFrequency = 0.0;
        double nominalFrequency = 0.0;

        foreach (SmbiosProcessorInfo cpu in cpus)
        {
            string? version = cpu.Version;
            if (!string.IsNullOrWhiteSpace(version))
                processorModelNames.Add(version!.Trim());

            if (cpu.CoreCount > 0)
                physicalCoreCount += cpu.CoreCount;
            else if (cpu.CoreEnabledCount > 0)
                physicalCoreCount += cpu.CoreEnabledCount;

            // Fall back to Thread Enabled when Thread Count is unknown.
            if (cpu.ThreadCount > 0)
                logicalCoreCount += cpu.ThreadCount;
            else if (cpu.ThreadEnabledCount > 0)
                logicalCoreCount += cpu.ThreadEnabledCount;

            if (cpu.MaxSpeedMHz > 0)
                maxFrequency = Math.Max(maxFrequency, cpu.MaxSpeedMHz);

            if (cpu.CurrentSpeedMHz > 0)
                nominalFrequency = nominalFrequency == 0
                    ? cpu.CurrentSpeedMHz
                    : Math.Min(nominalFrequency, cpu.CurrentSpeedMHz);
        }

        // Fall back to MaxSpeed when CurrentSpeed is unknown (and vice versa).
        if (nominalFrequency == 0)
            nominalFrequency = maxFrequency;
        if (maxFrequency == 0)
            maxFrequency = nominalFrequency;

        string? processorName = processorModelNames.Count > 0
            ? string.Join(", ", processorModelNames)
            : null;

        Frequency? maxFrequencyActual = maxFrequency > 0
            ? Frequency.FromMHz(maxFrequency)
            : null;
        Frequency? nominalFrequencyActual = nominalFrequency > 0
            ? Frequency.FromMHz(nominalFrequency)
            : null;

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = cpus.Count,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong()
        };
    }
}
