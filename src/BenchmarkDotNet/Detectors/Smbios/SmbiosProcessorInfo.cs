namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// Processor information from an SMBIOS Type 4 (Processor Information) structure.
/// See DSP0134 7.5. A speed of 0 means Unknown, and a count of 0 means Unknown.
/// </summary>
internal sealed class SmbiosProcessorInfo
{
    /// <summary>
    /// Socket designation string. Null when not provided.
    /// </summary>
    public string? SocketDesignation { get; init; }

    /// <summary>
    /// Processor manufacturer string. Null when not provided.
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Processor ID field as an uppercase hex string. Null when not provided.
    /// </summary>
    public string? ProcessorId { get; init; }

    /// <summary>
    /// Processor version string. Null when not provided.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// External clock frequency in MHz. 0 means Unknown.
    /// </summary>
    public ushort ExternalClockMHz { get; init; }

    /// <summary>
    /// Maximum configurable speed in MHz. 0 means Unknown.</summary>
    public ushort MaxSpeedMHz { get; init; }

    /// <summary>
    /// Current speed in MHz. 0 means Unknown.
    /// </summary>
    public ushort CurrentSpeedMHz { get; init; }

    /// <summary>
    /// Number of cores per processor. 0 means Unknown.
    /// </summary>
    public int CoreCount { get; init; }

    /// <summary>
    /// Number of enabled cores per processor. 0 means Unknown.
    /// </summary>
    public int CoreEnabledCount { get; init; }

    /// <summary>
    /// Number of threads per processor. 0 means Unknown.
    /// </summary>
    public int ThreadCount { get; init; }

    /// <summary>
    /// Number of firmware-enabled threads per processor (offset 0x30, SMBIOS 3.6+). 0 means Unknown.
    /// </summary>
    public int ThreadEnabledCount { get; init; }

    /// <summary>
    /// Socket type string (offset 0x32, SMBIOS 3.8+), e.g. "Socket LGA4710". Null when not provided.
    /// </summary>
    public string? SocketType { get; init; }

    /// <summary>
    /// Returns a human-readable single-line summary of this processor entry.
    /// Unknown values (missing strings or 0) are rendered as fallback text or "Unknown".
    /// </summary>
    public string ToDisplayString()
    {
        string socket = SocketDesignation.ToDisplayText("Unknown socket"); // 0x04
        string manufacturer = Manufacturer.ToDisplayText("Unknown manufacturer"); // 0x07
        string processorId = ProcessorId.ToDisplayText("Unknown"); // 0x08
        string name = Version.ToDisplayText("Unknown processor"); // 0x10
        string externalClock = ExternalClockMHz.ToDisplaySpeed(); // 0x12
        string maxSpeed = MaxSpeedMHz.ToDisplaySpeed(); // 0x14
        string currentSpeed = CurrentSpeedMHz.ToDisplaySpeed(); // 0x16
        string coreCount = CoreCount.ToDisplayCount(); // 0x23
        string coreEnabledCount = CoreEnabledCount.ToDisplayCount(); // 0x24
        string threadCount = ThreadCount.ToDisplayCount(); // 0x25
        string threadEnabledCount = ThreadEnabledCount.ToDisplayCount(); // 0x30
        string socketType = SocketType.ToDisplayText("Unknown"); // 0x32

        return $"{name} ({manufacturer}), " +
               $"Socket {socket} ({socketType}), " +
               $"External {externalClock}, " +
               $"Max {maxSpeed}, " +
               $"Current {currentSpeed}, " +
               $"{coreCount} cores ({coreEnabledCount} enabled), " +
               $"{threadCount} threads ({threadEnabledCount} enabled), ID {processorId}";
    }
    public override string ToString()
        => ToDisplayString();
}
