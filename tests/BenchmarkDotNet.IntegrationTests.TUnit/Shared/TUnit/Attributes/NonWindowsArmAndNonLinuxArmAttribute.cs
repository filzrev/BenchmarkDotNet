using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Tests;

public class NonWindowsArmAndNonLinuxArmAttribute()
    : SkipAttribute("This test does not support ARM on Windows or Linux")
{
    public override async Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        if (IsArm() && (OsDetector.IsWindows() || OsDetector.IsLinux()))
            return true;

        return false;
    }

    private static bool IsArm()
       => BenchmarkDotNet.Portability.RuntimeInformation.GetCurrentPlatform()
            is Platform.Arm64
            or Platform.Arm
            or Platform.Armv6;
}

