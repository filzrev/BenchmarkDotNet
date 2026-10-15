using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Smbios;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public class SmbiosMemoryDeviceTests(ITestOutputHelper Output)
{
    [Fact]
    public void SmbiosParser_GetMemoryInfo_OnWindows_ReturnsInstalledModules()
    {
        if (!OsDetector.IsWindows7OrLater())
            return;

        // Act
        IReadOnlyList<SmbiosMemoryDeviceInfo> modules = SmbiosParser.GetMemoryInfo();

        // Assert
        modules.Should().NotBeNull();

        // Output collected memory information.
        foreach (SmbiosMemoryDeviceInfo module in modules)
        {
            Output.WriteLine(module.ToDisplayString());
        }
    }

    [Fact]
    public void SmbiosMemoryDeviceInfo_ToDisplayString_FullEntry_ReturnsExpectedString()
    {
        // Arrange
        var module = new SmbiosMemoryDeviceInfo
        {
            SizeBytes = 8L * 1024 * 1024 * 1024, // 0x0C + 0x1C
            FormFactorId = 0x09,                 // 0x0E
            DeviceLocator = "DIMM A1",           // 0x10
            BankLocator = "BANK 0",              // 0x11
            MemoryTypeId = 0x1A,                 // 0x12
            SpeedMTs = 3200,                     // 0x15 + 0x54
            Manufacturer = "Samsung",            // 0x17
            SerialNumber = "TESTSERIAL",         // 0x18
            PartNumber = "M378A1K43CB2-CRC",     // 0x1A
            Rank = 2,                            // 0x1B
            ConfiguredSpeedMTs = 3200,           // 0x20 + 0x58
        };

        // Act
        string actual = module.ToDisplayString();

        // Assert
        actual.Should().Be(
            "DIMM A1 (BANK 0): 8 GB DDR4 DIMM, Samsung, Part M378A1K43CB2-CRC, " +
            "Speed 3200 MT/s (Configured 3200 MT/s), Rank 2, S/N TESTSERIAL");
        module.ToString().Should().Be(actual);
    }

    [Fact]
    public void SmbiosMemoryDeviceInfo_ToDisplayString_UnknownValues_RendersUnknown()
    {
        // Arrange
        var module = new SmbiosMemoryDeviceInfo();

        // Act
        string actual = module.ToDisplayString();

        // Assert
        actual.Should().Be(
            "Unknown (Unknown): Unknown Reserved Reserved, Unknown manufacturer, Part Unknown, " +
            "Speed Unknown (Configured Unknown), Rank Unknown, S/N Unknown");
    }

    [Fact]
    public void SmbiosMemoryDeviceInfo_ToDisplayString_KnownSpdIds_AppendsIds()
    {
        // Arrange
        var module = new SmbiosMemoryDeviceInfo
        {
            SizeBytes = 8L * 1024 * 1024 * 1024, // 0x0C + 0x1C
            FormFactorId = 0x09,                 // 0x0E
            DeviceLocator = "DIMM A1",           // 0x10
            BankLocator = "BANK 0",              // 0x11
            MemoryTypeId = 0x22,                 // 0x12
            SpeedMTs = 4800,                     // 0x15 + 0x54
            Manufacturer = "Samsung",            // 0x17
            SerialNumber = "TESTSERIAL",         // 0x18
            PartNumber = "M323R2GA3BB0",         // 0x1A
            Rank = 1,                            // 0x1B
            ConfiguredSpeedMTs = 4800,           // 0x20 + 0x58
            Pmic0ManufacturerId = 0x02CE,        // 0x5C
            Pmic0RevisionNumber = 0x0012,        // 0x5E
            RcdManufacturerId = 0x00B3,          // 0x60
            RcdRevisionNumber = 0x0034,          // 0x62
        };

        // Act
        string actual = module.ToDisplayString();

        // Assert
        actual.Should().Be(
            "DIMM A1 (BANK 0): 8 GB DDR5 DIMM, Samsung, Part M323R2GA3BB0, " +
            "Speed 4800 MT/s (Configured 4800 MT/s), Rank 1, S/N TESTSERIAL, " +
            "PMIC 0x02CE, PMIC rev 0x0012, RCD 0x00B3, RCD rev 0x0034");
    }

    [Theory]
    [InlineData((ushort)0, "Unknown")]
    [InlineData((ushort)1200, "1.2 V")]
    [InlineData((ushort)1350, "1.35 V")]
    [InlineData((ushort)1100, "1.1 V")]
    public void SmbiosDisplayExtensions_ToDisplayVoltage_FormatsMillivolts(ushort millivolts, string expected)
    {
        // Act
        string actual = SmbiosDisplayExtensions.ToDisplayVoltage(millivolts);

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData((byte)0x00, "Reserved")]
    [InlineData((byte)0x02, "Unknown")]
    [InlineData((byte)0x18, "DDR3")]
    [InlineData((byte)0x1A, "DDR4")]
    [InlineData((byte)0x22, "DDR5")]
    [InlineData((byte)0x23, "LPDDR5")]
    [InlineData((byte)0x24, "HBM3")]
    [InlineData((byte)0x25, "MRDIMM")]
    [InlineData((byte)0x26, "LPDDR6")]
    [InlineData((byte)0x27, "Unknown (0x27)")]
    public void SmbiosParser_MemoryTypeNames_MatchSpecification(byte id, string expected)
    {
        // Act
        var result = SmbiosParser.GetMemoryTypeName(id);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData((byte)0x09, "DIMM")]
    [InlineData((byte)0x0D, "SODIMM")]
    [InlineData((byte)0x10, "Die")]
    [InlineData((byte)0x11, "CAMM")]
    [InlineData((byte)0x12, "CUDIMM")]
    [InlineData((byte)0x13, "CSODIMM")]
    [InlineData((byte)0x00, "Reserved")]
    [InlineData((byte)0x14, "Unknown (0x14)")]
    public void SmbiosParser_FormFactorNames_MatchSpecification(byte id, string expected)
    {
        // Act
        string actual = SmbiosParser.GetFormFactorName(id);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void SmbiosParser_ParseTable_SingleDdr4Module_ReturnsExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(
            size: 8192, speed: 3200, configuredSpeed: 3200, rank: 2,
            memoryType: 0x1A, typeDetail: 0x2080,
            totalWidth: 64, dataWidth: 64,
            minVoltage: 1200, maxVoltage: 1200, configuredVoltage: 1200,
            technology: 0x03, operatingMode: 0x0008,
            volatileSize: 8UL * 1024 * 1024 * 1024, cacheSize: 0);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        SmbiosMemoryDeviceInfo module = result[0];
        module.DeviceLocator.Should().Be("DIMM A1");
        module.BankLocator.Should().Be("BANK 0");
        module.Manufacturer.Should().Be("Samsung");
        module.SerialNumber.Should().Be("TESTSERIAL");
        module.AssetTag.Should().Be("ATAG0123");
        module.PartNumber.Should().Be("M378A1K43CB2-CRC");
        module.FirmwareVersion.Should().Be("01");
        module.FormFactorId.Should().Be(0x09);
        module.FormFactorName.Should().Be("DIMM");
        module.MemoryTypeId.Should().Be(0x1A);
        module.MemoryTypeName.Should().Be("DDR4");
        module.TypeDetail.Should().Be(0x2080);
        module.TypeDetailNames.Should().Equal("Synchronous", "Registered (Buffered)");
        module.DeviceSet.Should().Be(1);
        module.Rank.Should().Be(2);
        module.TotalWidthBits.Should().Be(64);
        module.DataWidthBits.Should().Be(64);
        module.SizeBytes.Should().Be(8L * 1024 * 1024 * 1024);
        module.SpeedMTs.Should().Be(3200);
        module.ConfiguredSpeedMTs.Should().Be(3200);
        module.MinimumVoltageMillivolts.Should().Be(1200);
        module.MaximumVoltageMillivolts.Should().Be(1200);
        module.ConfiguredVoltageMillivolts.Should().Be(1200);
        module.MemoryTechnologyId.Should().Be(0x03);
        module.MemoryTechnologyName.Should().Be("DRAM");
        module.OperatingModeCapability.Should().Be(0x0008);
        module.OperatingModeNames.Should().Equal("Volatile memory");
        module.ModuleManufacturerId.Should().Be(0x02CE);
        module.ModuleProductId.Should().Be(0x1234);
        module.NonVolatileSizeBytes.Should().BeNull();
        module.VolatileSizeBytes.Should().Be(8UL * 1024 * 1024 * 1024);
        module.CacheSizeBytes.Should().Be(0UL);
        module.LogicalSizeBytes.Should().BeNull();
        module.ToDisplayString().Should().Be(
            "DIMM A1 (BANK 0): 8 GB DDR4 DIMM, Samsung, Part M378A1K43CB2-CRC, " +
            "Speed 3200 MT/s (Configured 3200 MT/s), Rank 2, S/N TESTSERIAL");
    }

    [Fact]
    public void SmbiosParser_ParseTable_ExtendedSize_UsesExtendedSize()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(size: 0x7FFF, extendedSizeMb: 32768);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].SizeBytes.Should().Be(32768L * 1024 * 1024);
    }

    [Fact]
    public void SmbiosParser_ParseTable_SizeInKilobytes_DecodesBytes()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(size: 0x8000 | 256);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].SizeBytes.Should().Be(256L * 1024);
    }

    [Fact]
    public void SmbiosParser_ParseTable_EmptySlot_IsSkipped()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(size: 0);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SmbiosParser_ParseTable_UnknownSize_IsIncludedWithUnknownBytes()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(size: 0xFFFF);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].SizeBytes.Should().Be(0);
    }

    [Fact]
    public void SmbiosParser_ParseTable_ExtendedSpeed_UsesExtendedSpeed()
    {
        // Arrange
        byte[] extended = SmbiosTables.BuildType17(speed: 0xFFFF, extendedSpeedMTs: 4800, configuredSpeed: 0xFFFF);
        byte[] capped = SmbiosTables.BuildType17(speed: 3200, extendedSpeedMTs: 9999);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(extended, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].SpeedMTs.Should().Be(4800);
        result[0].ConfiguredSpeedMTs.Should().Be(0);

        result.Clear();

        // Act
        SmbiosParser.ParseTable(capped, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].SpeedMTs.Should().Be(3200);
    }

    [Fact]
    public void SmbiosParser_ParseTable_PmicAndRcdFields_ReturnsExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(
            pmic0ManufacturerId: 0x02CE,
            pmic0Revision: 0x0012,
            rcdManufacturerId: 0x00B3,
            rcdRevision: 0x0034);

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].Pmic0ManufacturerId.Should().Be(0x02CE);
        result[0].Pmic0RevisionNumber.Should().Be(0x0012);
        result[0].RcdManufacturerId.Should().Be(0x00B3);
        result[0].RcdRevisionNumber.Should().Be(0x0034);
    }

    [Fact]
    public void SmbiosParser_ParseTable_Pre37ShortStructure_RevisionFieldsAreUnknown()
    {
        // Arrange
        byte[] full = SmbiosTables.BuildType17(
            pmic0ManufacturerId: 0x02CE,
            pmic0Revision: 0x0012,
            rcdManufacturerId: 0x00B3,
            rcdRevision: 0x0034);

        // Truncate the formatted area to the pre-3.7 length (0x5C): the 3.7 fields are absent.
        // Array.Copy is used instead of range slicing for net472 compatibility.
        const int fullLength = 0x64;
        const int shortLength = 0x5C;
        byte[] table = new byte[full.Length - (fullLength - shortLength)];
        Array.Copy(full, 0, table, 0, shortLength);
        Array.Copy(full, fullLength, table, shortLength, full.Length - fullLength);
        table[1] = shortLength;

        var result = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].Pmic0ManufacturerId.Should().Be(0);
        result[0].Pmic0RevisionNumber.Should().Be(0xFF00);
        result[0].RcdManufacturerId.Should().Be(0);
        result[0].RcdRevisionNumber.Should().Be(0xFF00);
    }
}
