using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Detectors.Smbios;

internal static partial class SmbiosDataAccessor
{
    /// <summary>
    /// Gets processor information (SMBIOS Type 4) for every populated socket.
    /// </summary>
    public static IReadOnlyList<SmbiosProcessorInfo> GetProcessorInfo()
    {
        if (!TryGetSmbiosData(out _, out var tableData))
            return [];

        return SmbiosDataParser.ParseProcessorInfoTable(tableData);
    }

    public static IReadOnlyList<SmbiosMemoryDeviceInfo> GetMemoryDeviceInfo()
    {
        if (!TryGetSmbiosData(out _, out var tableData))
            return [];

        return SmbiosDataParser.ParseMemoryDevicesInfo(tableData);
    }

    internal static bool TryGetSmbiosData(
        [NotNullWhen(true)] out Version? smbiosVersion,
        out ReadOnlySpan<byte> smbiosData)
    {
        if (OsDetector.IsWindows7OrLater())
            return WindowsDataAccessor.TryGetSmbiosData(out smbiosVersion, out smbiosData);

        if (OsDetector.IsLinux())
            return LinuxDataAccessor.TryGetSmbiosData(out smbiosVersion, out smbiosData);

        // SMBIOS is not available on `macos(arm64)` environment.
        // It can support SMBIOS on macos(x64) though it's not implemented.

        // Return false on other OS.
        smbiosVersion = null;
        smbiosData = [];
        return false;
    }
}
