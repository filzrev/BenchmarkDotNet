namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Memory device information from an SMBIOS Type 17 (Memory Device) structure.
/// See DSP0134 7.18.
/// Properties follow Table 75 offset order. Derived names follow the raw properties.
/// </summary>
internal sealed class SmbiosMemoryDeviceInfo
{
    /// <summary>
    /// Total width in bits, including any check or error-correction bits (offset 0x08).
    /// 0 means Unknown (spec: FFFFh).
    /// </summary>
    public ushort TotalWidthBits { get; init; }

    /// <summary>
    /// Data width in bits (offset 0x0A).
    /// 0 means Unknown (spec: FFFFh).
    /// </summary>
    public ushort DataWidthBits { get; init; }

    /// <summary>
    /// Installed capacity in bytes, decoded from Size (offset 0x0C) and
    /// Extended Size (offset 0x1C). 0 means Unknown or not installed.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Form factor identifier (offset 0x0E). See DSP0134 7.18.1.
    /// </summary>
    public byte FormFactorId { get; init; }

    /// <summary>
    /// Device set number (offset 0x0F). 0 means not part of a set, 0xFF means Unknown.
    /// </summary>
    public byte DeviceSet { get; init; }

    /// <summary>
    /// Physically-labeled socket string (offset 0x10). Null when not provided.
    /// </summary>
    public string? DeviceLocator { get; init; }

    /// <summary>
    /// Physically-labeled bank string (offset 0x11). Null when not provided.
    /// </summary>
    public string? BankLocator { get; init; }

    /// <summary>
    /// Memory type identifier (offset 0x12). See DSP0134 7.18.2.
    /// </summary>
    public byte MemoryTypeId { get; init; }

    /// <summary>
    /// Additional detail bitfield (offset 0x13). See DSP0134 7.18.3.
    /// Bit 0 is reserved.
    /// </summary>
    public ushort TypeDetail { get; init; }

    /// <summary>
    /// Maximum capable speed in MT/s, decoded from Speed (offset 0x15) and
    /// Extended Speed (offset 0x54). 0 means Unknown.
    /// </summary>
    public uint SpeedMTs { get; init; }

    /// <summary>
    /// Manufacturer string (offset 0x17). Null when not provided.
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Serial number string set by the manufacturer (offset 0x18). Null when not provided.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Asset tag string (offset 0x19). Null when not provided.
    /// </summary>
    public string? AssetTag { get; init; }

    /// <summary>
    /// Part number string set by the manufacturer (offset 0x1A). Null when not provided.
    /// </summary>
    public string? PartNumber { get; init; }

    /// <summary>
    /// Physical/package rank decoded from Attributes bits 4:0 (offset 0x1B).
    /// 0 means Unknown.
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Configured speed in MT/s, decoded from Configured Memory Speed (offset 0x20) and
    /// Extended Configured Memory Speed (offset 0x58). 0 means Unknown.
    /// </summary>
    public uint ConfiguredSpeedMTs { get; init; }

    /// <summary>
    /// Minimum operating voltage in millivolts (offset 0x22). 0 means Unknown.
    /// </summary>
    public ushort MinimumVoltageMillivolts { get; init; }

    /// <summary>
    /// Maximum operating voltage in millivolts (offset 0x24). 0 means Unknown.
    /// </summary>
    public ushort MaximumVoltageMillivolts { get; init; }

    /// <summary>
    /// Configured voltage in millivolts (offset 0x26). 0 means Unknown.
    /// </summary>
    public ushort ConfiguredVoltageMillivolts { get; init; }

    /// <summary>
    /// Memory technology type identifier (offset 0x28). See DSP0134 7.18.6.
    /// </summary>
    public byte MemoryTechnologyId { get; init; }

    /// <summary>
    /// Supported operating mode bitfield (offset 0x29). See DSP0134 7.18.7.
    /// This reports supported modes, not the current mode. Bit 0 is reserved.
    /// </summary>
    public ushort OperatingModeCapability { get; init; }

    /// <summary>
    /// Firmware version string (offset 0x2B). Null when not provided.
    /// </summary>
    public string? FirmwareVersion { get; init; }

    /// <summary>
    /// Two-byte module manufacturer ID from SPD, LSB first (offset 0x2C).
    /// See DSP0134 7.18.8. 0 means Unknown.
    /// </summary>
    public ushort ModuleManufacturerId { get; init; }

    /// <summary>
    /// Two-byte module product ID from SPD, LSB first (offset 0x2E).
    /// See DSP0134 7.18.9. 0 means Unknown.
    /// </summary>
    public ushort ModuleProductId { get; init; }

    /// <summary>
    /// Two-byte memory subsystem controller manufacturer ID from SPD, LSB first (offset 0x30).
    /// See DSP0134 7.18.10. 0 means Unknown.
    /// </summary>
    public ushort ControllerManufacturerId { get; init; }

    /// <summary>
    /// Two-byte memory subsystem controller product ID from SPD, LSB first (offset 0x32).
    /// See DSP0134 7.18.11. 0 means Unknown.
    /// </summary>
    public ushort ControllerProductId { get; init; }

    /// <summary>
    /// Non-volatile portion size in bytes (offset 0x34).
    /// Null means Unknown. 0 means None.
    /// </summary>
    public ulong? NonVolatileSizeBytes { get; init; }

    /// <summary>
    /// Volatile portion size in bytes (offset 0x3C).
    /// Null means Unknown. 0 means None.
    /// </summary>
    public ulong? VolatileSizeBytes { get; init; }

    /// <summary>
    /// Cache portion size in bytes (offset 0x44).
    /// Null means Unknown. 0 means None.
    /// </summary>
    public ulong? CacheSizeBytes { get; init; }

    /// <summary>
    /// Logical device size in bytes (offset 0x4C).
    /// Null means Unknown.
    /// </summary>
    public ulong? LogicalSizeBytes { get; init; }

    /// <summary>
    /// Two-byte PMIC0 manufacturer ID from SPD, LSB first (offset 0x5C, SMBIOS 3.7+).
    /// See DSP0134 7.18.15. 0 means Unknown.
    /// </summary>
    public ushort Pmic0ManufacturerId { get; init; }

    /// <summary>
    /// PMIC0 revision number from SPD (offset 0x5E, SMBIOS 3.7+).
    /// See DSP0134 7.18.16. 0xFF00 means Unknown.
    /// </summary>
    public ushort Pmic0RevisionNumber { get; init; }

    /// <summary>
    /// Two-byte RCD manufacturer ID from SPD, LSB first (offset 0x60, SMBIOS 3.7+).
    /// See DSP0134 7.18.17. 0 means Unknown.
    /// </summary>
    public ushort RcdManufacturerId { get; init; }

    /// <summary>
    /// RCD revision number from SPD (offset 0x62, SMBIOS 3.7+).
    /// See DSP0134 7.18.18. 0xFF00 means Unknown.
    /// </summary>
    public ushort RcdRevisionNumber { get; init; }

    public string FormFactorName
        => SmbiosParser.GetFormFactorName(FormFactorId);

    public string MemoryTypeName
        => SmbiosParser.GetMemoryTypeName(MemoryTypeId);

    public string MemoryTechnologyName
        => SmbiosParser.GetMemoryTechnologyName(MemoryTechnologyId);

    public IReadOnlyList<string> TypeDetailNames
        => SmbiosParser.GetTypeDetailNames(TypeDetail);

    public IReadOnlyList<string> OperatingModeNames => SmbiosParser.GetOperatingModeNames(OperatingModeCapability);

    /// <summary>
    /// Returns a human-readable single-line summary of this memory device.
    /// Unknown values (missing strings or 0) are rendered as fallback text or "Unknown".
    /// PMIC/RCD identifiers are appended only when at least one of them is known.
    /// </summary>
    public string ToDisplayString()
    {
        string locator = DeviceLocator.ToDisplayText("Unknown"); // 0x10
        string bank = BankLocator.ToDisplayText("Unknown"); // 0x11
        string manufacturer = Manufacturer.ToDisplayText("Unknown manufacturer"); // 0x17
        string serial = SerialNumber.ToDisplayText("Unknown"); // 0x18
        string part = PartNumber.ToDisplayText("Unknown"); // 0x1A

        string result = $"{locator} ({bank}): {SizeBytes.ToDisplaySize()} {MemoryTypeName} {FormFactorName}, " +
               $"{manufacturer}, Part {part}, " +
               $"Speed {SpeedMTs.ToDisplayMemorySpeed()} (Configured {ConfiguredSpeedMTs.ToDisplayMemorySpeed()}), " +
               $"{Rank.ToDisplayRank()}, S/N {serial}";

        string pmicRcdIds = ToDisplayPmicRcdIds();
        return pmicRcdIds.Length == 0 ? result : $"{result}, {pmicRcdIds}";
    }

    private string ToDisplayPmicRcdIds()
    {
        // Revision 0x0000 is treated as Unknown for display, following the
        // codebase convention that 0 means Unknown (0xFF00 is the spec sentinel).
        List<string> ids = new();
        if (Pmic0ManufacturerId != 0)
            ids.Add($"PMIC 0x{Pmic0ManufacturerId:X4}");
        if (Pmic0RevisionNumber != 0 && Pmic0RevisionNumber != 0xFF00)
            ids.Add($"PMIC rev 0x{Pmic0RevisionNumber:X4}");
        if (RcdManufacturerId != 0)
            ids.Add($"RCD 0x{RcdManufacturerId:X4}");
        if (RcdRevisionNumber != 0 && RcdRevisionNumber != 0xFF00)
            ids.Add($"RCD rev 0x{RcdRevisionNumber:X4}");
        return string.Join(", ", ids);
    }

    public override string ToString()
        => ToDisplayString();
}
