using System.Buffers.Binary;
using System.Runtime.Versioning;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Parser for raw SMBIOS data obtained via GetSystemFirmwareTable(RSMB).
/// Definitions are based on DSP0134 (SMBIOS Reference Specification) 3.10.0
/// </summary>
internal partial class SmbiosParser
{
    private const FIRMWARE_TABLE_PROVIDER RSMB = FIRMWARE_TABLE_PROVIDER.RSMB; // 0x52534D42;

    private const int RsmbHeaderSize = 8;
    private const int TableLengthOffset = 4;

    private readonly record struct SmbiosStructure(
        SmbiosType Type,
        int Offset,
        byte Length,
        int StringSetStart,
        int NextOffset
    );

    /// <summary>
    /// Gets the raw SMBIOS (RSMB) firmware table.
    /// </summary>
    [SupportedOSPlatform("windows6.0.6000")]
    public static byte[] GetRawSmbiosData()
    {
        // Query required buffer size.
        uint requiredSize = PInvoke.GetSystemFirmwareTable(RSMB, 0, []);
        if (requiredSize == 0 || requiredSize < RsmbHeaderSize)
            return [];

        // Gets SMBIOS data.
        var buffer = new byte[requiredSize];
        uint written = PInvoke.GetSystemFirmwareTable(RSMB, 0, buffer.AsSpan());
        if (written == 0 || written < RsmbHeaderSize)
            return [];

        // Resize buffer when written length is lower than buffer size.
        if (written < buffer.Length)
            Array.Resize(ref buffer, (int)written);

        return buffer;
    }

    /// <summary>
    /// Reads the SMBIOS structure table and returns its location within the raw firmware table.
    /// </summary>
    [SupportedOSPlatform("windows6.0.6000")]
    private static bool TryGetSmbiosTable(out byte[] buffer, out int offset, out int length)
    {
        buffer = GetRawSmbiosData();
        offset = RsmbHeaderSize;
        length = 0;
        if (buffer.Length < RsmbHeaderSize)
            return false;

        uint tableLength = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan().Slice(TableLengthOffset));
        if (tableLength == 0)
            return false;

        // Defensive: firmware may report a length larger than the buffer we received.
        length = (int)Math.Min(tableLength, (uint)(buffer.Length - RsmbHeaderSize));
        return true;
    }

    /// <summary>
    /// Gets SMBIOS information entries of the specified structure <paramref name="type"/>.
    /// The type argument <typeparamref name="T"/> must correspond to <paramref name="type"/>
    /// (<see cref="SmbiosProcessorInfo"/> for <see cref="SmbiosType.ProcessorInformation"/>,
    /// <see cref="SmbiosMemoryDeviceInfo"/> for <see cref="SmbiosType.MemoryDevice"/>).
    /// </summary>
    [SupportedOSPlatform("windows6.0.6000")]
    public static IReadOnlyList<T> GetInfo<T>(SmbiosType type)
    {
        if (!TryGetSmbiosTable(out byte[] buffer, out int offset, out int length))
            return [];

        var result = new List<T>();
        ParseTable<T>(buffer.AsSpan(offset, length), type, result);
        return result;
    }

    internal static void ParseTable<T>(byte[] data, SmbiosType type, List<T> result)
    {
        ParseTable<T>(data.AsSpan(), type, result);
    }

    internal static void ParseTable<T>(ReadOnlySpan<byte> data, SmbiosType type, List<T> result)
    {
        switch (type)
        {
            case SmbiosType.ProcessorInformation when result is List<SmbiosProcessorInfo> processors:
                ParseTable(data, processors);
                break;
            case SmbiosType.MemoryDevice when result is List<SmbiosMemoryDeviceInfo> modules:
                ParseTable(data, modules);
                break;
            default:
                throw new ArgumentException($"Type argument '{typeof(T).Name}' does not correspond to '{type}'.", nameof(type));
        }
    }

    private static List<SmbiosStructure> EnumerateStructures(ReadOnlySpan<byte> data, SmbiosType? wantedType = null)
    {
        var structures = new List<SmbiosStructure>();
        int offset = 0;
        while (offset + 4 <= data.Length)
        {
            SmbiosType type = (SmbiosType)data[offset];
            byte length = data[offset + 1];

            // Length includes the 4-byte header (type/length/handle).
            if (length < 4 || offset + length > data.Length)
                break;

            int stringSetStart = offset + length;
            if (!TryFindNextStructureOffset(data, stringSetStart, out int nextOffset))
                break;

            if (type == SmbiosType.EndOfTable)
                break;

            // Create instances only for the requested type.
            if (wantedType is null || type == wantedType)
                structures.Add(new SmbiosStructure(type, offset, length, stringSetStart, nextOffset));

            offset = nextOffset;
        }

        return structures;
    }

    private static bool TryFindNextStructureOffset(ReadOnlySpan<byte> data, int stringSetStart, out int nextOffset)
    {
        // String-set is a sequence of null-terminated strings, terminated by an extra null (double-null).
        // Scan for 0x00 0x00. If there is no string-set content, the structure is immediately
        // followed by 0x00 0x00.
        for (int i = stringSetStart; i + 1 < data.Length; i++)
        {
            if (data[i] == 0 && data[i + 1] == 0)
            {
                nextOffset = i + 2;
                return true;
            }
        }

        nextOffset = -1;
        return false;
    }

    private static IReadOnlyList<string> ReadStringSet(ReadOnlySpan<byte> data, int start, int end)
    {
        // end points just past the double-null terminator; the last two bytes are the terminator.
        if (start >= end)
            return [];

        List<string> strings = new();
        int stringStart = start;
        int terminatorStart = Math.Max(start, end - 2);
        for (int i = start; i < terminatorStart; i++)
        {
            if (data[i] != 0)
                continue;

            strings.Add(DecodeString(data[stringStart..i]));
            stringStart = i + 1;
        }

        // Parse last string before double-null.
        if (stringStart < terminatorStart)
            strings.Add(DecodeString(data[stringStart..terminatorStart]));

        return strings;
    }

    private static string DecodeString(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return "";
#if NET
        return Encoding.UTF8.GetString(data);
#else
        return Encoding.UTF8.GetString(data.ToArray());
#endif
    }
}
