using BenchmarkDotNet.Detectors;

namespace BenchmarkDotNet.Tests.Detectors.Smbios;

public class SmbiosTestBase
{
    protected readonly ITestOutputHelper Output;

    public SmbiosTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected static bool IsSmbiosAvailable()
    {
        if (OsDetector.IsWindows())
            return true;

#if NET
        if (OsDetector.IsLinux() && Environment.IsPrivilegedProcess)
            return true;
#endif

        return false;
    }
}
