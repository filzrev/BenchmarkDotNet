using AwesomeAssertions;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Cpu.Windows;
using BenchmarkDotNet.Detectors.Smbios;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public class SmbiosProcessorTests
{
    [Fact]
    public void SmbiosParser_GetCpuInfo_OnWindows_ReturnsPopulatedSockets()
    {
        if (!OsDetector.IsWindows7OrLater() || !OsDetector.IsLinux())
            return;

        // Act
        IReadOnlyList<SmbiosProcessorInfo> cpus = SmbiosParser.GetCpuInfo();

        // Assert
        cpus.Should().NotBeNull();
        foreach (SmbiosProcessorInfo cpu in cpus)
        {
            Console.WriteLine(cpu.ToDisplayString());
        }
    }

    [Fact]
    public void SmbiosCpuInfo_ToDisplayString_FullEntry_ReturnsExpectedString()
    {
        // Arrange
        var cpu = new SmbiosProcessorInfo
        {
            SocketDesignation = "AM4",                     // 0x04
            SocketType = "Socket AM4",                     // 0x32
            Manufacturer = "Advanced Micro Devices, Inc.", // 0x07
            Version = "AMD Ryzen 5 3500 6-Core Processor", // 0x10
            ProcessorId = "178BFBFF00870F10",              // 0x08
            ExternalClockMHz = 100,                        // 0x12
            MaxSpeedMHz = 4100,                            // 0x14
            CurrentSpeedMHz = 3600,                        // 0x16
            CoreCount = 6,                                 // 0x23 + 0x2A
            CoreEnabledCount = 6,                          // 0x24 + 0x2C
            ThreadCount = 6,                               // 0x25 + 0x2E
            ThreadEnabledCount = 6,                        // 0x30
        };

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().Be(
            "AMD Ryzen 5 3500 6-Core Processor (Advanced Micro Devices, Inc.), Socket AM4 (Socket AM4), " +
            "External 100MHz, Max 4100MHz, Current 3600MHz, " +
            "6 cores (6 enabled), 6 threads (6 enabled), ID 178BFBFF00870F10");
        cpu.ToString().Should().Be(actual);
    }

    [Fact]
    public void SmbiosCpuInfo_ToDisplayString_TrimsVersionPadding()
    {
        // Real firmware often pads the Version string with trailing spaces.
        // Arrange
        var cpu = new SmbiosProcessorInfo { Version = "AMD Ryzen 5 3500 6-Core Processor              " }; // 0x10

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().StartWith("AMD Ryzen 5 3500 6-Core Processor (");
    }

    [Fact]
    public void SmbiosCpuInfo_ToDisplayString_UnknownValues_RendersUnknown()
    {
        // Arrange
        var cpu = new SmbiosProcessorInfo();

        // Act
        string actual = cpu.ToDisplayString();

        // Assert
        actual.Should().Be(
            "Unknown processor (Unknown manufacturer), Socket Unknown socket (Unknown), " +
            "External Unknown, Max Unknown, Current Unknown, " +
            "Unknown cores (Unknown enabled), Unknown threads (Unknown enabled), ID Unknown");
    }

    [Fact]
    public void SmbiosParser_ParseTable_SingleProcessor_ReturnsExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType4(
            socket: "AM4", manufacturer: "Advanced Micro Devices, Inc.", version: "AMD Ryzen 5 3500 6-Core Processor",
            externalClockMHz: 100, maxSpeedMHz: 4100, currentSpeedMHz: 3600, status: 0x41,
            coreCount: 6, coreEnabled: 6, threadCount: 6);

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        SmbiosProcessorInfo cpu = result[0];
        cpu.SocketDesignation.Should().Be("AM4");
        cpu.Manufacturer.Should().Be("Advanced Micro Devices, Inc.");
        cpu.Version.Should().Be("AMD Ryzen 5 3500 6-Core Processor");
        cpu.ExternalClockMHz.Should().Be(100);
        cpu.MaxSpeedMHz.Should().Be(4100);
        cpu.CurrentSpeedMHz.Should().Be(3600);
        cpu.CoreCount.Should().Be(6);
        cpu.CoreEnabledCount.Should().Be(6);
        cpu.ThreadCount.Should().Be(6);
        cpu.ProcessorId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SmbiosParser_ParseTable_ExtendedCounts_UsesExtendedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType4(
            socket: "LGA1700", manufacturer: "Intel", version: "Core i9",
            externalClockMHz: 100, maxSpeedMHz: 5000, currentSpeedMHz: 4800, status: 0x41,
            coreCount: 0xFF, coreEnabled: 0xFF, threadCount: 0xFF,
            coreCount2: 32, coreEnabled2: 28, threadCount2: 56);

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].CoreCount.Should().Be(32);
        result[0].CoreEnabledCount.Should().Be(28);
        result[0].ThreadCount.Should().Be(56);
    }

    [Fact]
    public void SmbiosParser_ParseTable_ThreadEnabledAndSocketType_ReturnsExpectedValues()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType4(
            socket: "AM4", manufacturer: "AMD", version: "Ryzen",
            externalClockMHz: 100, maxSpeedMHz: 4100, currentSpeedMHz: 3600, status: 0x41,
            coreCount: 6, coreEnabled: 6, threadCount: 12,
            threadEnabled: 8, socketType: "Socket AM4");

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].ThreadEnabledCount.Should().Be(8);
        result[0].SocketType.Should().Be("Socket AM4");
    }

    [Fact]
    public void SmbiosParser_ParseTable_ThreadEnabledReserved_IsUnknown()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType4(
            socket: "AM4", manufacturer: "AMD", version: "Ryzen",
            externalClockMHz: 100, maxSpeedMHz: 4100, currentSpeedMHz: 3600, status: 0x41,
            coreCount: 6, coreEnabled: 6, threadCount: 12,
            threadEnabled: 0xFFFF);

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].ThreadEnabledCount.Should().Be(0);
    }

    [Fact]
    public void SmbiosParser_ParseTable_Pre36ShortStructure_NewFieldsAreUnknown()
    {
        // Arrange
        byte[] full = SmbiosTables.BuildType4(
            socket: "AM4", manufacturer: "AMD", version: "Ryzen",
            externalClockMHz: 100, maxSpeedMHz: 4100, currentSpeedMHz: 3600, status: 0x41,
            coreCount: 6, coreEnabled: 6, threadCount: 12,
            threadEnabled: 8, socketType: "Socket AM4");

        // Truncate the formatted area to the pre-3.6 length (0x30): the 3.6+ fields are absent.
        // Array.Copy is used instead of range slicing for net472 compatibility.
        const int fullLength = 0x33;
        const int shortLength = 0x30;
        byte[] table = new byte[full.Length - (fullLength - shortLength)];
        Array.Copy(full, 0, table, 0, shortLength);
        Array.Copy(full, fullLength, table, shortLength, full.Length - fullLength);
        table[1] = shortLength;

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().HaveCount(1);
        result[0].ThreadEnabledCount.Should().Be(0);
        result[0].SocketType.Should().BeNull();
    }

    [Fact]
    public void SmbiosParser_ParseTable_UnpopulatedSocket_IsSkipped()
    {
        // Arrange
        byte[] table = SmbiosTables.BuildType4(
            socket: "S1", manufacturer: "M", version: "V",
            externalClockMHz: 0, maxSpeedMHz: 0, currentSpeedMHz: 0, status: 0x00,
            coreCount: 8, coreEnabled: 8, threadCount: 16);

        var result = new List<SmbiosProcessorInfo>();

        // Act
        SmbiosParser.ParseTable(table, result);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SmbiosCpuDetector_FromSmbios_AggregatesSockets()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            new() { SocketDesignation = "CPU0", Manufacturer = "AMD", Version = "Ryzen", MaxSpeedMHz = 4100, CurrentSpeedMHz = 3600, CoreCount = 6, CoreEnabledCount = 6, ThreadCount = 6 },
            new() { SocketDesignation = "CPU1", Manufacturer = "AMD", Version = "Ryzen", MaxSpeedMHz = 4100, CurrentSpeedMHz = 3600, CoreCount = 6, CoreEnabledCount = 6, ThreadCount = 6 }
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromSmbios(cpus);

        // Assert
        cpuInfo.Should().NotBeNull();
        cpuInfo!.ProcessorName.Should().Be("Ryzen");
        cpuInfo.PhysicalProcessorCount.Should().Be(2);
        cpuInfo.PhysicalCoreCount.Should().Be(12);
        cpuInfo.LogicalCoreCount.Should().Be(12);
        cpuInfo.MaxFrequencyHz.Should().Be(4100L * 1000 * 1000);
        cpuInfo.NominalFrequencyHz.Should().Be(3600L * 1000 * 1000);
    }

    [Fact]
    public void SmbiosCpuDetector_FromSmbios_Empty_ReturnsNull()
    {
        // Act
        var cpuInfo = SmbiosCpuDetector.FromSmbios([]);

        // Assert
        cpuInfo.Should().BeNull();
    }

    [Fact]
    public void SmbiosCpuDetector_FromSmbios_UnknownThreadCount_FallsBackToThreadEnabled()
    {
        // Arrange
        var cpus = new List<SmbiosProcessorInfo>
        {
            new() { SocketDesignation = "CPU0", Manufacturer = "AMD", Version = "Ryzen", MaxSpeedMHz = 4100, CurrentSpeedMHz = 3600, CoreCount = 6, CoreEnabledCount = 6, ThreadCount = 0, ThreadEnabledCount = 8 }
        };

        // Act
        var cpuInfo = SmbiosCpuDetector.FromSmbios(cpus);

        // Assert
        cpuInfo.Should().NotBeNull();
        cpuInfo!.LogicalCoreCount.Should().Be(8);
    }
}
