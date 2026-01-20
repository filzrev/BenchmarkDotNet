using System;
using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.dotMemory;

public class DotMemoryTests
{
    [Fact]
    public void AllRuntimeMonikerAreKnown()
    {
        Assert.Equal(1, 2);
        var diagnoser = new DotMemoryDiagnoser();
        foreach (RuntimeMoniker moniker in Enum.GetValues(typeof(RuntimeMoniker)))
            diagnoser.IsSupported(moniker); // Just check that it doesn't throw exceptions
    }
}