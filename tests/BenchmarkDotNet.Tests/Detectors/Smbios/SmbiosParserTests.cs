using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Tests.XUnit;
using System.Buffers.Binary;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public partial class SmbiosParserTests(ITestOutputHelper Output)
{
    [FactEnvSpecific(EnvRequirement.WindowsOnly)]
    public void DumpRawSmbiosData()
    {
        if (!OsDetector.IsWindows7OrLater())
            return;

        byte[] data = SmbiosParser.GetRawSmbiosData();
        if (data.Length < 8)
        {
            Output.WriteLine("No SMBIOS data.");
            return;
        }

        /// Layout:
        /// [0] Used20CallingMethod
        /// [1] MajorVersion
        /// [2] MinorVersion
        /// [3] DmiRevision,
        /// [4..7] Length of the SMBIOS structure table (LE uint32),
        /// [8..] SMBIOS structures.

        // Output `{MajorVersion}.{MinorVersion}` and DmiRevision information.
        Output.WriteLine($"SMBIOS Version: {data[1]}.{data[2]} (DMI Revision: {data[3]})");

        // Output length.
        uint tableLength = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));
        Output.WriteLine($"Table Length: {tableLength:N0} bytes (Total: {data.Length:N0} bytes)");

        // Output bytes as Base64 text.
        Output.WriteLine($"Base64: {Convert.ToBase64String(data)}");
    }


    [Fact]
    public void SmbiosParser_GetRawSmbiosData_ReturnsTableHeader()
    {
        if (!OsDetector.IsWindows7OrLater())
            return;

        // Act
        var isSuccess = SmbiosParser.TryGetSmbiosTable(out var data,out var offset, out int length);

        // Assert
        isSuccess.Should().BeTrue();
        data.Length.Should().BeGreaterThan(8);
        data.Length.Should().Be(length);

        // Gets length of the SMBIOS table (4 byte, little-endian).
        uint tableLength = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));


        var cpus = new List<SmbiosProcessorInfo>();
        SmbiosParser.GetCpuInfo();
        SmbiosParser.ParseTable(data,SmbiosType.ProcessorInformation, cpus)
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0, 0, 0 })]
    [InlineData(new byte[] { 4, 2, 0, 0, 0, 0 })]
    public void SmbiosParser_ParseTable_InvalidData_DoesNotThrow(byte[] tableData)
    {
        // Arrange
        var result = new List<SmbiosProcessorInfo>();
        Action act = () => SmbiosParser.ParseTable(tableData, result);

        // Act
        act.Should().NotThrow();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SmbiosParser_ParseTable_Generic_ReturnsTypedEntries()
    {
        // Arrange
        byte[] cpu = SmbiosTables.BuildType4(
            socket: "AM4", manufacturer: "AMD", version: "Ryzen",
            externalClockMHz: 100, maxSpeedMHz: 4100, currentSpeedMHz: 3600, status: 0x41,
            coreCount: 6, coreEnabled: 6, threadCount: 6);
        byte[] memory = SmbiosTables.BuildType17(size: 8192);
        byte[] table = cpu.Concat(memory).Concat(new byte[] { 127, 4, 0, 0, 0, 0 }).ToArray();

        var cpus = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, SmbiosType.ProcessorInformation, cpus);

        // Assert
        cpus.Should().HaveCount(1);
        cpus[0].Version.Should().Be("Ryzen");

        var modules = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, SmbiosType.MemoryDevice, modules);

        // Assert
        modules.Should().HaveCount(1);
        modules[0].SizeBytes.Should().Be(8192L * 1024 * 1024);
    }

    [Fact]
    public void SmbiosParser_ParseTable_Generic_MismatchedType_Throws()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType17(size: 8192);

        var cpus = new List<SmbiosProcessorInfo>();

        // Act
        Action act = () => SmbiosParser.ParseTable(table, SmbiosType.MemoryDevice, cpus);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SmbiosParser_GetInfo_Generic_ReturnsSameEntriesAsSpecificMethods()
    {
        if (!OsDetector.IsWindows7OrLater())
            return;

        // Act
        IReadOnlyList<SmbiosProcessorInfo> expectedCpus = SmbiosParser.GetCpuInfo();
        IReadOnlyList<SmbiosProcessorInfo> actualCpus = SmbiosParser.GetInfo<SmbiosProcessorInfo>(SmbiosType.ProcessorInformation);

        // Assert
        actualCpus.Should().HaveCount(expectedCpus.Count);

        // Act
        IReadOnlyList<SmbiosMemoryDeviceInfo> expectedModules = SmbiosParser.GetMemoryInfo();
        IReadOnlyList<SmbiosMemoryDeviceInfo> actualModules = SmbiosParser.GetInfo<SmbiosMemoryDeviceInfo>(SmbiosType.MemoryDevice);

        // Assert
        actualModules.Should().HaveCount(expectedModules.Count);
    }

    [Fact]
    public void SmbiosParser_ParseTable_MixedCpuAndMemory_ParsesEachType()
    {
        // Arrange
        byte[] cpu = SmbiosTables.BuildType4(
            socket: "AM4",
            manufacturer: "AMD",
            version: "Ryzen",
            externalClockMHz: 100,
            maxSpeedMHz: 4100,
            currentSpeedMHz: 3600,
            status: 0x41,
            coreCount: 6,
            coreEnabled: 6,
            threadCount: 6);
        byte[] memory = SmbiosTables.BuildType17(size: 8192);
        byte[] table = cpu.Concat(memory).Concat(new byte[] { 127, 4, 0, 0, 0, 0 }).ToArray();

        var cpus = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, cpus);

        // Assert
        cpus.Should().HaveCount(1);

        var modules = new List<SmbiosMemoryDeviceInfo>();

        // Act
        SmbiosParser.ParseTable(table, modules);

        // Assert
        modules.Should().HaveCount(1);
        modules[0].SizeBytes.Should().Be(8192L * 1024 * 1024);
    }
}
