namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64DisassemblerTests : Arm64DisassemblerTestBase
{
    private const ulong DummyBaseAddress = 0x1000_0000_0000_0000UL;
    private const string DummyTargetFramework = "net10.0";

    public Arm64DisassemblerTests(ITestOutputHelper output)
        : base(output)
    {
    }
}
