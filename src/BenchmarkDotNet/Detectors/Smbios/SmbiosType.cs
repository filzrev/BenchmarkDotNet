namespace BenchmarkDotNet.Detectors.Smbios;

/// <summary>
/// SMBIOS structure types (structure type identifiers)
/// </summary>
internal enum SmbiosType : byte
{
    /// <summary>
    /// Processor Information (Type 4). See DSP0134 7.5.
    /// </summary>
    ProcessorInformation = 4,

    /// <summary>
    /// Memory Device (Type 17). See DSP0134 7.18.
    /// </summary>
    MemoryDevice = 17,

    /// <summary>
    /// End-of-Table (Type 127).
    /// </summary>
    EndOfTable = 127
}
