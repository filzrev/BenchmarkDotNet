using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Detectors.Smbios;

internal partial class SmbiosParser
{
    /// <summary>
    /// Gets memory device information (SMBIOS Type 17) for every populated slot.
    /// Slots without an installed module (Size = 0) are skipped.
    /// Supported on Windows (via RSMB) and Linux (via sysfs DMI tables).
    /// </summary>
    [SupportedOSPlatform("windows6.0.6000")]
    [SupportedOSPlatform("linux")]
    public static IReadOnlyList<SmbiosMemoryDeviceInfo> GetMemoryInfo()
    {
        if (!TryGetSmbiosTable(out byte[] buffer, out int offset, out int length))
            return [];

        var result = new List<SmbiosMemoryDeviceInfo>();
        ParseTable(buffer.AsSpan(offset, length), result);

        return result;
    }

    internal static void ParseTable(byte[] data, List<SmbiosMemoryDeviceInfo> result) => ParseTable(data.AsSpan(), result);

    internal static void ParseTable(ReadOnlySpan<byte> data, List<SmbiosMemoryDeviceInfo> result)
    {
        foreach (SmbiosStructure structure in EnumerateStructures(data, SmbiosType.MemoryDevice))
        {
            // Slice out the structure's formatted area (fixed-length fields).
            ReadOnlySpan<byte> bytes = data.Slice(structure.Offset, structure.Length);

            // Read the structure's string-set (unformatted area).
            var strings = ReadStringSet(data, structure.StringSetStart, structure.NextOffset);
            if (TryParseMemoryDeviceInfo(bytes, strings, out SmbiosMemoryDeviceInfo? memoryDeviceInfo))
                result.Add(memoryDeviceInfo);
        }
    }

    // Type 17 field offsets (DSP0134 7.18, Memory Device).
    // Each offset is bundled with its data width.
    // Handles (0x02 self, 0x04 array, 0x06 error-info) are intentionally omitted:
    // they are cross-structure links, not device attributes, and their targets
    // (Type 16/18) have no parser.
    private static class MemoryDeviceOffsets
    {
        internal static readonly UInt16Field TotalWidth = new(0x08);
        internal static readonly UInt16Field DataWidth = new(0x0A);
        internal static readonly UInt16Field Size = new(0x0C);
        internal static readonly ByteField FormFactor = new(0x0E);
        internal static readonly ByteField DeviceSet = new(0x0F);
        internal static readonly StringField DeviceLocator = new(0x10);
        internal static readonly StringField BankLocator = new(0x11);
        internal static readonly ByteField MemoryType = new(0x12);
        internal static readonly UInt16Field TypeDetail = new(0x13);
        internal static readonly UInt16Field Speed = new(0x15);
        internal static readonly StringField Manufacturer = new(0x17);
        internal static readonly StringField SerialNumber = new(0x18);
        internal static readonly StringField AssetTag = new(0x19);
        internal static readonly StringField PartNumber = new(0x1A);
        internal static readonly ByteField Attributes = new(0x1B);
        internal static readonly UInt32Field ExtendedSize = new(0x1C);
        internal static readonly UInt16Field ConfiguredSpeed = new(0x20);
        internal static readonly UInt16Field MinimumVoltage = new(0x22);
        internal static readonly UInt16Field MaximumVoltage = new(0x24);
        internal static readonly UInt16Field ConfiguredVoltage = new(0x26);
        internal static readonly ByteField MemoryTechnology = new(0x28);
        internal static readonly UInt16Field OperatingMode = new(0x29);
        internal static readonly StringField FirmwareVersion = new(0x2B);
        internal static readonly UInt16Field ModuleManufacturerId = new(0x2C);
        internal static readonly UInt16Field ModuleProductId = new(0x2E);
        internal static readonly UInt16Field ControllerManufacturerId = new(0x30);
        internal static readonly UInt16Field ControllerProductId = new(0x32);
        internal static readonly UInt64Field NonVolatileSize = new(0x34);
        internal static readonly UInt64Field VolatileSize = new(0x3C);
        internal static readonly UInt64Field CacheSize = new(0x44);
        internal static readonly UInt64Field LogicalSize = new(0x4C);
        internal static readonly UInt32Field ExtendedSpeed = new(0x54);
        internal static readonly UInt32Field ExtendedConfiguredSpeed = new(0x58);
        internal static readonly UInt16Field Pmic0ManufacturerId = new(0x5C);
        internal static readonly UInt16Field Pmic0RevisionNumber = new(0x5E, 0xFF00);
        internal static readonly UInt16Field RcdManufacturerId = new(0x60);
        internal static readonly UInt16Field RcdRevisionNumber = new(0x62, 0xFF00);
    }

    private static bool TryParseMemoryDeviceInfo(
        ReadOnlySpan<byte> bytes,
        IReadOnlyList<string> strings,
        [NotNullWhen(true)] out SmbiosMemoryDeviceInfo? memoryDeviceInfo)
    {
        // Type 17 offsets are relative to the structure start (DSP0134 7.18, Memory Device).
        // Every access is bounds-checked because older SMBIOS revisions have shorter formatted areas.
        ushort size = MemoryDeviceOffsets.Size.Read(bytes);
        if (size == 0)
        {
            memoryDeviceInfo = null;
            return false; // No Module Installed
        }

        bool hasExtendedSize = MemoryDeviceOffsets.ExtendedSize.HasField(bytes);
        uint extendedSizeMb = MemoryDeviceOffsets.ExtendedSize.Read(bytes);

        // Guard on the later field: it implies the contiguous ExtendedSpeed field is present too.
        bool hasExtendedSpeed = MemoryDeviceOffsets.ExtendedConfiguredSpeed.HasField(bytes);
        uint speedMTs = DecodeSpeed(MemoryDeviceOffsets.Speed.Read(bytes), MemoryDeviceOffsets.ExtendedSpeed.Read(bytes), hasExtendedSpeed);
        uint configuredSpeedMTs = DecodeSpeed(MemoryDeviceOffsets.ConfiguredSpeed.Read(bytes), MemoryDeviceOffsets.ExtendedConfiguredSpeed.Read(bytes), hasExtendedSpeed);

        memoryDeviceInfo = new SmbiosMemoryDeviceInfo
        {
            TotalWidthBits = NormalizeWidth(MemoryDeviceOffsets.TotalWidth.Read(bytes)),           // 0x08
            DataWidthBits = NormalizeWidth(MemoryDeviceOffsets.DataWidth.Read(bytes)),             // 0x0A
            SizeBytes = DecodeSizeBytes(size, extendedSizeMb, hasExtendedSize),                    // 0x0C + 0x1C
            FormFactorId = MemoryDeviceOffsets.FormFactor.Read(bytes),                             // 0x0E
            DeviceSet = MemoryDeviceOffsets.DeviceSet.Read(bytes),                                 // 0x0F
            DeviceLocator = MemoryDeviceOffsets.DeviceLocator.Read(bytes, strings),                // 0x10
            BankLocator = MemoryDeviceOffsets.BankLocator.Read(bytes, strings),                    // 0x11
            MemoryTypeId = MemoryDeviceOffsets.MemoryType.Read(bytes),                             // 0x12
            TypeDetail = MemoryDeviceOffsets.TypeDetail.Read(bytes),                               // 0x13
            SpeedMTs = speedMTs,                                                                   // 0x15 + 0x54
            Manufacturer = MemoryDeviceOffsets.Manufacturer.Read(bytes, strings),                  // 0x17
            SerialNumber = MemoryDeviceOffsets.SerialNumber.Read(bytes, strings),                  // 0x18
            AssetTag = MemoryDeviceOffsets.AssetTag.Read(bytes, strings),                          // 0x19
            PartNumber = MemoryDeviceOffsets.PartNumber.Read(bytes, strings),                      // 0x1A
            Rank = MemoryDeviceOffsets.Attributes.Read(bytes) & 0x0F,                              // 0x1B
            ConfiguredSpeedMTs = configuredSpeedMTs,                                               // 0x20 + 0x58
            MinimumVoltageMillivolts = MemoryDeviceOffsets.MinimumVoltage.Read(bytes),             // 0x22
            MaximumVoltageMillivolts = MemoryDeviceOffsets.MaximumVoltage.Read(bytes),             // 0x24
            ConfiguredVoltageMillivolts = MemoryDeviceOffsets.ConfiguredVoltage.Read(bytes),       // 0x26
            MemoryTechnologyId = MemoryDeviceOffsets.MemoryTechnology.Read(bytes),                 // 0x28
            OperatingModeCapability = MemoryDeviceOffsets.OperatingMode.Read(bytes),               // 0x29
            FirmwareVersion = MemoryDeviceOffsets.FirmwareVersion.Read(bytes, strings),            // 0x2B
            ModuleManufacturerId = MemoryDeviceOffsets.ModuleManufacturerId.Read(bytes),           // 0x2C
            ModuleProductId = MemoryDeviceOffsets.ModuleProductId.Read(bytes),                     // 0x2E
            ControllerManufacturerId = MemoryDeviceOffsets.ControllerManufacturerId.Read(bytes),   // 0x30
            ControllerProductId = MemoryDeviceOffsets.ControllerProductId.Read(bytes),             // 0x32
            NonVolatileSizeBytes = MemoryDeviceOffsets.NonVolatileSize.Read(bytes),                // 0x34
            VolatileSizeBytes = MemoryDeviceOffsets.VolatileSize.Read(bytes),                      // 0x3C
            CacheSizeBytes = MemoryDeviceOffsets.CacheSize.Read(bytes),                            // 0x44
            LogicalSizeBytes = MemoryDeviceOffsets.LogicalSize.Read(bytes),                        // 0x4C
            Pmic0ManufacturerId = MemoryDeviceOffsets.Pmic0ManufacturerId.Read(bytes),             // 0x5C
            Pmic0RevisionNumber = MemoryDeviceOffsets.Pmic0RevisionNumber.Read(bytes),             // 0x5E
            RcdManufacturerId = MemoryDeviceOffsets.RcdManufacturerId.Read(bytes),                 // 0x60
            RcdRevisionNumber = MemoryDeviceOffsets.RcdRevisionNumber.Read(bytes),                 // 0x62
        };

        return true;
    }

    private static long DecodeSizeBytes(ushort size, uint extendedSizeMb, bool hasExtendedSize)
    {
        const long KB = 1024L;
        const long MB = 1024L * 1024;
        if (size == 0 || size == 0xFFFF)
            return 0; // No Module Installed, or Unknown

        if (size == 0x7FFF && hasExtendedSize)
            return (extendedSizeMb & 0x7FFFFFFFu) * MB; // Extended Size is in MB

        if ((size & 0x8000) != 0)
            return (size & 0x7FFF) * KB; // Bit 15 set: value is in KB

        return size * MB; // Otherwise the value is in MB
    }

    private static ushort NormalizeWidth(ushort width)
        => width == 0xFFFF ? (ushort)0 : width; // 0xFFFF and 0 both mean Unknown

    private static uint DecodeSpeed(ushort speed, uint extendedSpeed, bool hasExtendedSpeed)
    {
        // Speed is in MT/s. 0xFFFF selects the extended speed field (SMBIOS 3.3+); 0 means Unknown.
        if (speed == 0xFFFF)
            return hasExtendedSpeed ? extendedSpeed : 0;
        return speed;
    }

    // DSP0134 7.18.1, Memory Device Form Factor.
    // Definitions are based on SMBIOS Specification 3.10.0
    internal static string GetFormFactorName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "SIMM",
        0x04 => "SIP",
        0x05 => "Chip",
        0x06 => "DIP",
        0x07 => "ZIP",
        0x08 => "Proprietary Card",
        0x09 => "DIMM",
        0x0A => "TSOP",
        0x0B => "Row Of Chips",
        0x0C => "RIMM",
        0x0D => "SODIMM",
        0x0E => "SRIMM",
        0x0F => "FB-DIMM",
        0x10 => "Die",
        0x11 => "CAMM",
        0x12 => "CUDIMM",
        0x13 => "CSODIMM",
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.2, Memory Device Type. Values above 0x26 are out of spec.
    internal static string GetMemoryTypeName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "DRAM",
        0x04 => "EDRAM",
        0x05 => "VRAM",
        0x06 => "SRAM",
        0x07 => "RAM",
        0x08 => "ROM",
        0x09 => "Flash",
        0x0A => "EEPROM",
        0x0B => "FEPROM",
        0x0C => "EPROM",
        0x0D => "CDRAM",
        0x0E => "3DRAM",
        0x0F => "SDRAM",
        0x10 => "SGRAM",
        0x11 => "RDRAM",
        0x12 => "DDR",
        0x13 => "DDR2",
        0x14 => "DDR2 FB-DIMM",
        0x15 => "Reserved",
        0x16 => "Reserved",
        0x17 => "Reserved",
        0x18 => "DDR3",
        0x19 => "FBD2",
        0x1A => "DDR4",
        0x1B => "LPDDR",
        0x1C => "LPDDR2",
        0x1D => "LPDDR3",
        0x1E => "LPDDR4",
        0x1F => "Logical non-volatile device",
        0x20 => "HBM",
        0x21 => "HBM2",
        0x22 => "DDR5",
        0x23 => "LPDDR5",
        0x24 => "HBM3",
        0x25 => "MRDIMM",
        0x26 => "LPDDR6",
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.3, Memory Device Type Detail (bitfield, bit 0 is reserved).
    internal static IReadOnlyList<string> GetTypeDetailNames(ushort detail)
    {
        string[] names =
        [
            "Other",
            "Unknown",
            "Fast-paged",
            "Static Column",
            "Pseudo-static",
            "RAMBus",
            "Synchronous",
            "CMOS",
            "EDO",
            "Window DRAM",
            "Cache DRAM",
            "Non-Volatile",
            "Registered (Buffered)",
            "Unbuffered (Unregistered)",
            "LRDIMM",
        ];

        var result = new List<string>();
        for (int bit = 1; bit <= 15; bit++)
        {
            if ((detail & (1 << bit)) != 0)
                result.Add(names[bit - 1]);
        }

        return result;
    }

    // DSP0134 7.18.6, Memory Technology.
    internal static string GetMemoryTechnologyName(byte id) => id switch
    {
        0x00 => "Reserved",
        0x01 => "Other",
        0x02 => "Unknown",
        0x03 => "DRAM",
        0x04 => "NVDIMM-N",
        0x05 => "NVDIMM-F",
        0x06 => "NVDIMM-P",
        0x07 => "Intel Optane DC persistent memory",
        0x08 => "MRDIMM", // Deprecated
        _ => $"Unknown (0x{id:X2})"
    };

    // DSP0134 7.18.7, Memory Operating Mode Capability (bitfield, bit 0 is reserved).
    internal static IReadOnlyList<string> GetOperatingModeNames(ushort capability)
    {
        string[] names =
        [
            "Other",
            "Unknown",
            "Volatile memory",
            "Byte-accessible persistent memory",
            "Block-accessible persistent memory",
        ];
        var result = new List<string>();
        for (int bit = 1; bit <= 5; bit++)
        {
            if ((capability & (1 << bit)) != 0)
                result.Add(names[bit - 1]);
        }

        return result;
    }
}
