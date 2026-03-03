using BenchmarkDotNet.Reports;
using System.Linq;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning;

public static class Extensions
{
    public static void CheckPlatformLinkerIssues(this Summary summary)
    {
        if (summary.Reports.Any(r =>
                !r.BuildResult.IsBuildSuccess &&
                r.BuildResult.ErrorMessage.Contains("Platform linker not found")))
            throw new MisconfiguredEnvironmentException("Failed to build benchmarks because the platform linker not found");
    }
}
