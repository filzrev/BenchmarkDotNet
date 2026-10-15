using System.Text;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

internal static class SmbiosTables
{
    internal static byte[] BuildType4(
        string socket,
        string manufacturer,
        string version,
        ushort externalClockMHz,
        ushort maxSpeedMHz,
        ushort currentSpeedMHz,
        byte status,
        byte coreCount,
        byte coreEnabled,
        byte threadCount,
        ushort coreCount2 = 0,
        ushort coreEnabled2 = 0,
        ushort threadCount2 = 0,
        ushort threadEnabled = 0,
        string? socketType = null)
    {
        const byte length = 0x33;
        var buffer = new List<byte>
        {
            4,       // 0x00: type
            length,  // 0x01: length
            0x01,    // 0x02: handle
            0x00,
            1,       // 0x04: Socke Designation
            3,       // 0x05: Processor Type
            0x2B,    // 0x06: Processor Family
            2,       // 0x07: manufacturer string index
        };
        buffer.AddRange([0x17, 0x8B, 0xFB, 0xFF, 0x00, 0x87, 0x0F, 0x10]); // 0x08-0x0F: Processor ID
        buffer.Add(3);                                                     // 0x10: Processor Version
        buffer.Add(0x88);                                                  // 0x11: Voltage
        AddU16(buffer, externalClockMHz);          // 0x12-0x13: External Clock
        AddU16(buffer, maxSpeedMHz);               // 0x14-0x15: Max Speed
        AddU16(buffer, currentSpeedMHz);           // 0x16-0x17: Current Speed
        buffer.Add(status);                                                // 0x18: Status
        buffer.Add(4);                                                     // 0x19: Processor Upgrade
        buffer.AddRange([0xFF, 0xFF]);                                     // 0x1A-0x1B: L1 Cache Handle
        buffer.AddRange([0xFF, 0xFF]);                                     // 0x1C-0x1D: L2 Cache Handle
        buffer.AddRange([0xFF, 0xFF]);                                     // 0x1E-0x1F: L3 Cache Handle
        buffer.AddRange([0]);                                              // 0x20: Serial Number
        buffer.AddRange([0]);                                              // 0x21: Asset Tag
        buffer.AddRange([0]);                                              // 0x22: Part Number
        buffer.Add(coreCount);                                             // 0x23: Core Count
        buffer.Add(coreEnabled);                                           // 0x24: Core Enabled
        buffer.Add(threadCount);                                           // 0x25: Thread Count
        AddU16(buffer, (ushort)0x0201);            // 0x26-0x27: Processor Characteristics
        AddU16(buffer, (ushort)0x2B);              // 0x28-0x29: Processor Family 2
        AddU16(buffer, coreCount2);                // 0x2A-0x2B: Core Count 2
        AddU16(buffer, coreEnabled2);              // 0x2C-0x2D: Core Enabled 2
        AddU16(buffer, threadCount2);              // 0x2E-0x2F: Thread Count 2
        AddU16(buffer, threadEnabled);             // 0x30-0x31: Thread Enabled (3.6+)
        buffer.Add(socketType is null ? (byte)0 : (byte)4);                // 0x32: Socket Type (3.8+)

        // Write the string-set. Each string is null-terminated, and the set is double-null-terminated.
        if (socketType is null)
            WriteStringSet(buffer, socket, manufacturer, version);
        else
            WriteStringSet(buffer, socket, manufacturer, version, socketType);

        return buffer.ToArray();
    }

    internal static byte[] BuildType17(
        ushort size = 8192,
        uint extendedSizeMb = 0,
        ushort speed = 3200,
        uint extendedSpeedMTs = 0,
        ushort configuredSpeed = 3200,
        uint extendedConfiguredSpeedMTs = 0,
        byte formFactor = 0x09,
        byte memoryType = 0x1A,
        ushort typeDetail = 0x2080,
        byte rank = 2,
        ushort totalWidth = 64,
        ushort dataWidth = 64,
        ushort minVoltage = 1200,
        ushort maxVoltage = 1200,
        ushort configuredVoltage = 1200,
        byte technology = 0x03,
        ushort operatingMode = 0x0008,
        ulong nonVolatileSize = ulong.MaxValue,
        ulong volatileSize = ulong.MaxValue,
        ulong cacheSize = ulong.MaxValue,
        ulong logicalSize = ulong.MaxValue,
        ushort pmic0ManufacturerId = 0,
        ushort pmic0Revision = 0xFF00,
        ushort rcdManufacturerId = 0,
        ushort rcdRevision = 0xFF00)
    {
        const byte length = 0x64;
        var buffer = new List<byte>
        {
            17,      // 0x00: type
            length,  // 0x01: length
            0x10,    // 0x02: handle
            0x00,    // 0x03:
            0x0F,    // 0x04: Physical Memory Array Handle
            0x00,    // 0x05:
            0xFF,    // 0x06: Memory Error Information Handle (No Error)
            0xFF,    // 0x07:
        };
        AddU16(buffer, totalWidth);          // 0x08-0x09: Total Width
        AddU16(buffer, dataWidth);           // 0x0A-0x0B: Data Width
        AddU16(buffer, size);                // 0x0C-0x0D: Size
        buffer.Add(formFactor);                                      // 0x0E: Form Factor
        buffer.Add(1);                                               // 0x0F: Device Set
        buffer.Add(1);                                               // 0x10: Device Locator
        buffer.Add(2);                                               // 0x11: Bank Locator
        buffer.Add(memoryType);                                      // 0x12: Memory Type
        AddU16(buffer, typeDetail);          // 0x13-0x14: Type Detail
        AddU16(buffer, speed);               // 0x15-0x16: Speed
        buffer.Add(3);                                               // 0x17: Manufacturer
        buffer.Add(4);                                               // 0x18: Serial Number
        buffer.Add(5);                                               // 0x19: Asset Tag
        buffer.Add(6);                                               // 0x1A: Part Number
        buffer.Add((byte)(rank & 0x0F));                             // 0x1B: Attributes (Rank in bits 3:0)
        AddU32(buffer, extendedSizeMb);      // 0x1C-0x1F: Extended Size
        AddU16(buffer, configuredSpeed);     // 0x20-0x21: Configured Memory Speed
        AddU16(buffer, minVoltage);          // 0x22-0x23: Minimum Voltage
        AddU16(buffer, maxVoltage);          // 0x24-0x25: Maximum Voltage
        AddU16(buffer, configuredVoltage);   // 0x26-0x27: Configured Voltage
        buffer.Add(technology);                                      // 0x28: Memory Technology
        AddU16(buffer, operatingMode);       // 0x29-0x2A: Memory Operating Mode Capability
        buffer.Add(7);                                               // 0x2B: Firmware Version
        AddU16(buffer, (ushort)0x02CE);      // 0x2C-0x2D: Module Manufacturer ID
        AddU16(buffer, (ushort)0x1234);      // 0x2E-0x2F: Module Product ID
        AddU16(buffer, (ushort)0);           // 0x30-0x31: Controller Manufacturer ID
        AddU16(buffer, (ushort)0);           // 0x32-0x33: Controller Product ID
        AddU64(buffer, nonVolatileSize);     // 0x34-0x3B: Non-Volatile Size
        AddU64(buffer, volatileSize);        // 0x3C-0x43: Volatile Size
        AddU64(buffer, cacheSize);           // 0x44-0x4B: Cache Size
        AddU64(buffer, logicalSize);         // 0x4C-0x53: Logical Size
        AddU32(buffer, extendedSpeedMTs);    // 0x54-0x57: Extended Speed
        AddU32(buffer, extendedConfiguredSpeedMTs); // 0x58-0x5B: Extended Configured Speed
        AddU16(buffer, pmic0ManufacturerId); // 0x5C-0x5D: PMIC0 Manufacturer ID (3.7+)
        AddU16(buffer, pmic0Revision);       // 0x5E-0x5F: PMIC0 Revision Number (3.7+)
        AddU16(buffer, rcdManufacturerId);   // 0x60-0x61: RCD Manufacturer ID (3.7+)
        AddU16(buffer, rcdRevision);         // 0x62-0x63: RCD Revision Number (3.7+)

        // Write the string-set. Each string is null-terminated, and the set is double-null-terminated.
        WriteStringSet(buffer,
        [
            "DIMM A1",          // 1
            "BANK 0",           // 2
            "Samsung",          // 3
            "TESTSERIAL",       // 4
            "ATAG0123",         // 5
            "M378A1K43CB2-CRC", // 6
            "01"
        ]);

        return buffer.ToArray();
    }

    // SMBIOS integers are little-endian. Bytes are emitted explicitly so the
    // builder produces identical tables on big-endian hosts (BitConverter.GetBytes
    // follows host byte order and must not be used here).
    private static void AddU16(List<byte> buffer, ushort value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)(value >> 8));
    }

    private static void AddU32(List<byte> buffer, uint value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)(value >> 24));
    }

    private static void AddU64(List<byte> buffer, ulong value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 24) & 0xFF));
        buffer.Add((byte)((value >> 32) & 0xFF));
        buffer.Add((byte)((value >> 40) & 0xFF));
        buffer.Add((byte)((value >> 48) & 0xFF));
        buffer.Add((byte)(value >> 56));
    }

    private static void WriteStringSet(List<byte> buffer, params string[] strings)
    {
        foreach (string s in strings)
        {
            buffer.AddRange(Encoding.UTF8.GetBytes(s));
            buffer.Add(0);
        }
        buffer.Add(0); // end of string-set (double-null)
    }
}
