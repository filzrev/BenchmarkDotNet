using System;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning;

public class MisconfiguredEnvironmentException(string message) : Exception(message)
{
    public string SkipMessage => $"Skip this test because the environment is misconfigured ({Message})";
}
