using BenchmarkDotNet.Disassemblers;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests : Arm64DisassemblerTestBase
{
    // On macos(arm64), available virtual address space is 48-bit (or 52-bit) and minimum address must be 4GB (0x1_0000_0000)
    private const ulong DummyBaseAddress = 0x0000_F000_0000_0000UL;
    private const string DummyTargetFramework = "net10.0";

    // 4GB as base address. it's minimum valid address on macos(arm64) 
    private const ulong Address1 = 0x1_0000_0000;
    private const ulong Address2 = 0x2_0000_0000;
    private const ulong Address3 = 0x3_0000_0000;

    public Arm64DisassemblerTests(ITestOutputHelper output)
        : base(output)
    {
    }
}
