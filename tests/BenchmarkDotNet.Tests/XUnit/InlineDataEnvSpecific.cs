using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace BenchmarkDotNet.Tests.XUnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlineDataEnvSpecific : DataAttribute
{
    /// <summary>
    /// Gets the data to be passed to the test.
    /// </summary>
    // If the user passes null to the constructor, we assume what they meant was a
    // single null value to be passed to the test.
    public object?[] Data { get; }

    public InlineDataEnvSpecific(object? data, string reason, params EnvRequirement[] requirements)
        : this([data], reason, requirements) { }

    public InlineDataEnvSpecific(object?[] data, string reason, params EnvRequirement[] requirements)
    {
        // If the user passes null to the constructor, we assume what they meant was a
        // single null value to be passed to the test.
        Data = data ?? [null];
        string? skip = EnvRequirementChecker.GetSkip(requirements);
        if (skip != null)
            Skip = $"{skip} ({reason})";
    }

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker)
    {
        var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        TestIntrospectionHelper.MergeTraitsInto(traits, Traits);

        return new([
            new TheoryDataRow(Data)
            {
                Explicit = ExplicitAsNullable,
                Label = Label,
                Skip = Skip,
                TestDisplayName = TestDisplayName,
                Timeout = TimeoutAsNullable,
                Traits = traits,
            }
        ]);
    }

    ////public override async ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
    ////{
    ////    // InlineDataEnvSpecific has two constructors:
    ////    // 1. InlineDataEnvSpecific(object data, string reason, params EnvRequirement[] requirements)
    ////    // 2. InlineDataEnvSpecific(object[] data, string reason, params EnvRequirement[] requirements)
    ////    // GetConstructorArguments returns arguments from the constructor that was actually called

    ////    var args = data;
    ////    if (args.Length == 0)
    ////        yield break;

    ////    // First argument is either a single object or object[] - wrap accordingly
    ////    if (args[0] is object[] dataArray)
    ////    {
    ////        // Array constructor was used
    ////        yield return dataArray;
    ////    }
    ////    else
    ////    {
    ////        // Single object constructor was used - wrap in array
    ////        yield return new[] { args[0] };
    ////    }
    ////}

    public override bool SupportsDiscoveryEnumeration()
        => true;
}
