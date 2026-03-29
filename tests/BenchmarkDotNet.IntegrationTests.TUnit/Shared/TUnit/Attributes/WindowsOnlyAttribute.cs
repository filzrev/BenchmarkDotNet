using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Tests;

public class WindowsOnlyAttribute()
    : SkipAttribute("This test is only supported on Windows")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        return Task.FromResult(!isWindows);
    }
}

