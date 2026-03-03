using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Loggers;
using TUnit.Core.Interfaces;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning;

public class OutputLogger : AccumulationLogger
{
    private readonly ITestOutput testOutputHelper;
    private string currentLine = "";

    public OutputLogger(ITestOutput testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void Write(LogKind logKind, string text)
    {
        currentLine += text;
        base.Write(logKind, text);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void WriteLine()
    {
        testOutputHelper.WriteLine(currentLine);
        currentLine = "";
        base.WriteLine();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void WriteLine(LogKind logKind, string text)
    {
        testOutputHelper.WriteLine(currentLine + text);
        currentLine = "";
        base.WriteLine(logKind, text);
    }
}
