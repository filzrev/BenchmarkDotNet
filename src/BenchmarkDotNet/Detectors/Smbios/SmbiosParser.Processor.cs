using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

#if NETSTANDARD2_0
using System.Text;
#endif

namespace BenchmarkDotNet.Detectors.Smbios;

internal partial class SmbiosParser
{
    /// <summary>
    /// Gets processor information (SMBIOS Type 4) for every populated socket.
    /// Supported on Windows (via RSMB) and Linux (via sysfs DMI tables).
    /// </summary>
    [SupportedOSPlatform("windows6.0.6000")]
    [SupportedOSPlatform("linux")]
    public static IReadOnlyList<SmbiosProcessorInfo> GetCpuInfo()
    {
        if (!TryGetSmbiosTable(out byte[] buffer, out int offset, out int length))
            return [];

        var result = new List<SmbiosProcessorInfo>();
        ParseTable(buffer.AsSpan(offset, length), result);

        return result;
    }

    internal static void ParseTable(byte[] data, List<SmbiosProcessorInfo> result) => ParseTable(data.AsSpan(), result);

    internal static void ParseTable(ReadOnlySpan<byte> data, List<SmbiosProcessorInfo> result)
    {
        foreach (SmbiosStructure structure in EnumerateStructures(data, SmbiosType.ProcessorInformation))
        {
            // Slice out the structure's formatted area (fixed-length fields).
            ReadOnlySpan<byte> bytes = data.Slice(structure.Offset, structure.Length);
            // Read the structure's string-set (unformatted area).
            var strings = ReadStringSet(data, structure.StringSetStart, structure.NextOffset);
            if (TryParseProcessorInfo(bytes, strings, out SmbiosProcessorInfo? cpu))
                result.Add(cpu);
        }
    }

    // Type 4 field offsets (DSP0134 7.5, Processor Information).
    // Each offset is bundled with its data width.
    private static class ProcessorOffsets
    {
        internal static readonly StringField SocketDesignation = new(0x04);
        internal static readonly StringField Manufacturer = new(0x07);
        internal static readonly StringField Version = new(0x10);
        internal static readonly UInt16Field ExternalClock = new(0x12);
        internal static readonly UInt16Field MaxSpeed = new(0x14);
        internal static readonly UInt16Field CurrentSpeed = new(0x16);
        internal static readonly ByteField Status = new(0x18);
        internal static readonly ByteField CoreCount = new(0x23);
        internal static readonly ByteField CoreEnabled = new(0x24);
        internal static readonly ByteField ThreadCount = new(0x25);
        internal static readonly UInt16Field CoreCount2 = new(0x2A);
        internal static readonly UInt16Field CoreEnabled2 = new(0x2C);
        internal static readonly UInt16Field ThreadCount2 = new(0x2E);
        internal static readonly UInt16Field ThreadEnabled = new(0x30);
        internal static readonly StringField SocketType = new(0x32);
    }

    // Status (offset 0x18) bit 6: CPU Socket Populated.
    private const byte StatusPopulatedMask = 0x40;

    private static bool TryParseProcessorInfo(
        ReadOnlySpan<byte> bytes,
        IReadOnlyList<string> strings,
        [NotNullWhen(true)] out SmbiosProcessorInfo? processorInfo)
    {
        // Type 4 offsets are relative to the structure start. Every access is bounds-checked
        // because older SMBIOS revisions have shorter formatted areas.
        ushort externalClockMHz = ProcessorOffsets.ExternalClock.Read(bytes);
        ushort maxSpeedMHz = ProcessorOffsets.MaxSpeed.Read(bytes);
        ushort currentSpeedMHz = ProcessorOffsets.CurrentSpeed.Read(bytes);

        byte status = ProcessorOffsets.Status.Read(bytes);
        bool hasStatus = ProcessorOffsets.Status.HasField(bytes);

        // Bit 6 of Status indicates whether the socket is populated.
        if (hasStatus && (status & StatusPopulatedMask) == 0)
        {
            processorInfo = null;
            return false;
        }

        byte coreCount = ProcessorOffsets.CoreCount.Read(bytes);
        byte coreEnabled = ProcessorOffsets.CoreEnabled.Read(bytes);
        byte threadCount = ProcessorOffsets.ThreadCount.Read(bytes);

        ushort coreCount2 = ProcessorOffsets.CoreCount2.Read(bytes);
        ushort coreEnabled2 = ProcessorOffsets.CoreEnabled2.Read(bytes);
        ushort threadCount2 = ProcessorOffsets.ThreadCount2.Read(bytes);

        // 0x2A-0x2F are supported SMBIOS 3.0+
        bool hasExtendedCounts = ProcessorOffsets.ThreadCount2.HasField(bytes);
        int cores = ResolveCount(coreCount, coreCount2, hasExtendedCounts);
        int coresEnabled = ResolveCount(coreEnabled, coreEnabled2, hasExtendedCounts);
        int threads = ResolveCount(threadCount, threadCount2, hasExtendedCounts);

        string? processorId = TryReadProcessorId(bytes);

        // Thread Enabled (DSP0134 7.5.10, SMBIOS 3.6+): FFFFh is reserved → Unknown.
        ushort threadEnabledRaw = ProcessorOffsets.ThreadEnabled.Read(bytes);
        int threadEnabled = threadEnabledRaw == 0xFFFF ? 0 : threadEnabledRaw;

        processorInfo = new SmbiosProcessorInfo
        {
            SocketDesignation = ProcessorOffsets.SocketDesignation.Read(bytes, strings),  // 0x04
            Manufacturer = ProcessorOffsets.Manufacturer.Read(bytes, strings),            // 0x07
            ProcessorId = processorId,                                                    // 0x08
            Version = ProcessorOffsets.Version.Read(bytes, strings),                      // 0x10
            ExternalClockMHz = externalClockMHz,                                          // 0x12
            MaxSpeedMHz = maxSpeedMHz,                                                    // 0x14
            CurrentSpeedMHz = currentSpeedMHz,                                            // 0x16
            CoreCount = cores,                                                            // 0x23 + 0x2A
            CoreEnabledCount = coresEnabled,                                              // 0x24 + 0x2C
            ThreadCount = threads,                                                        // 0x25 + 0x2E
            ThreadEnabledCount = threadEnabled,                                           // 0x30
            SocketType = ProcessorOffsets.SocketType.Read(bytes, strings),                // 0x32
        };

        return true;
    }

    private static int ResolveCount(byte shortCount, ushort extendedCount, bool hasExtended)
    {
        // Table 26: FFh without the extended field means 255 (not "see extended").
        if (shortCount == 0xFF && hasExtended)
            return extendedCount == 0xFFFF ? 0 : extendedCount; // FFFFh is reserved → Unknown
        if (shortCount == 0xFF)
            return 255;
        return shortCount; // 0 means unknown
    }

    private static string? TryReadProcessorId(ReadOnlySpan<byte> bytes)
    {
        const int relativeOffset = 0x08;
        const int size = 8;
        if (relativeOffset + size > bytes.Length)
            return null;

        // Convert to hex string (Processor ID is 8-bytes and architecture(e.g. x64/arm64) specific format)
#if NET
        return Convert.ToHexString(bytes.Slice(relativeOffset, size));
#else
        ReadOnlySpan<byte> id = bytes.Slice(relativeOffset, size);
        var builder = new StringBuilder(size * 2);
        foreach (byte b in id)
            builder.Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        return builder.ToString();
#endif
    }
}
