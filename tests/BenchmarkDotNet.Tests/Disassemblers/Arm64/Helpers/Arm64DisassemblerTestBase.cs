using Microsoft.Diagnostics.Runtime;
using System.Buffers.Binary;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public abstract class Arm64DisassemblerTestBase
{
    protected readonly ITestOutputHelper Output;

    public Arm64DisassemblerTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected void PrintInstructions(Gee.External.Capstone.Arm64.Arm64Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            Output.WriteLine(instruction.ToString());
        }
    }

    protected void PrintInstructions(uint[] rawInstructions)
    {
        foreach (var rawinstruction in rawInstructions)
        {
            var instruction = rawinstruction.ToCapstoneArm64Instruction();
            Output.WriteLine(instruction.ToString());
        }
    }

    protected static MockClrRuntime CreateMockClrRuntime(ulong dummyValue)
    {
        return new MockClrRuntime(new MockDataTarget(new MockDataReader(dummyValue)));
    }

    protected static MockClrRuntime CreateMockClrRuntime()
        => CreateMockClrRuntime([]);

    protected static MockClrRuntime CreateMockClrRuntime(uint[] rawInstructions)
    {
        Func<ulong, ulong> dummyFunc = _ => throw new InvalidOperationException("This func is not expected to be called.");
        return CreateMockClrRuntime(rawInstructions, dummyFunc);
    }

    protected static MockClrRuntime CreateMockClrRuntime(uint[] rawInstructions, Func<ulong, ulong> getPointer)
    {
        var dataReader = CreateMockDataReader(rawInstructions, getPointer);
        return new MockClrRuntime(new MockDataTarget(dataReader));
    }

    protected static IDataReader CreateMockDataReader(uint[] rawInstructions)
    {
        Func<ulong, ulong> dummyFunc = _ => throw new InvalidOperationException("This func is not expected to be called.");
        return CreateMockDataReader(rawInstructions, dummyFunc);
    }

    protected static IDataReader CreateMockDataReader(uint[] rawInstructions, Func<ulong, ulong> getPointer)
    {
        return new MockDataReader(
            read: (address, buffer) =>
            {
                var instructionCountToWrite = Math.Min(rawInstructions.Length, buffer.Length / 4);

                for (int i = 0; i < instructionCountToWrite; i++)
                {
                    uint rawInstruction = rawInstructions[i];
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(i * 4), rawInstruction);
                }

                return instructionCountToWrite * 4;
            },
            tryReadPointer: new TryReadPointerDelegate((address, out value) =>
            {
                value = getPointer(address);
                return true;
            })
        );
    }
}
