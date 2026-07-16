using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.Models.MSBuild;

internal class ReferenceCopyLocalPaths : List<ReferenceCopyLocalPath>
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, // Use `Disallow` for unmapped member testing
        AllowDuplicateProperties = false,
        IndentSize = 2,
        Converters =
        {
            new BooleanConverter(),
            new DateTimeOffsetConverter(),
            new ReferenceCopyLocalPathConverter(),
        },
    };

    public RuntimeAsset[] GetRuntimeAssets()
        => this.OfType<RuntimeAsset>().ToArray();

    public ResourceAsset[] GetResourceAssets()
        => this.OfType<ResourceAsset>().ToArray();

    public NativeAsset[] GetNativeAssets()
        => this.OfType<NativeAsset>().ToArray();

    public ProjectReference[] GetProjectReferences()
        => this.OfType<ProjectReference>().ToArray();

    public ReferenceCopyLocalPathForUnknown[] UnknownItems
        => this.OfType<ReferenceCopyLocalPathForUnknown>().ToArray();
}

internal abstract class ReferenceCopyLocalPath
{
    public required string Identity { get; init; }

    public required bool CopyLocal { get; init; }

    public required string FullPath { get; init; }

    public required string RootDir { get; init; }

    public required string Filename { get; init; }

    public required string Extension { get; init; }

    public required string RelativeDir { get; init; }

    public required string Directory { get; init; }

    public required string RecursiveDir { get; init; }

    public required DateTimeOffset ModifiedTime { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset AccessedTime { get; init; }

    public required string DefiningProjectFullPath { get; init; }

    public required string DefiningProjectDirectory { get; init; }

    public required string DefiningProjectName { get; init; }

    public required string DefiningProjectExtension { get; init; }
}

internal abstract class AssetBase : ReferenceCopyLocalPath
{
    public required string AssetType { get; init; }

    public required string PathInPackage { get; init; }

    public string DestinationSubPath { get; init; } = "";

    public string DestinationSubDirectory { get; init; } = "";

    public required string NuGetPackageId { get; init; }

    public required string NuGetPackageVersion { get; init; }
}

internal class RuntimeAsset : AssetBase
{
    public string RuntimeIdentifier { get; init; } = "";
}

internal class ResourceAsset : AssetBase
{
    public required string Culture { get; init; }
}

internal class NativeAsset : AssetBase
{
    public string RuntimeIdentifier { get; init; } = "";
}

internal class ProjectReference : ReferenceCopyLocalPath
{
    public required string ReferenceSourceTarget { get; init; }

    public required string MSBuildSourceProjectFile { get; init; }

    public required string HasSingleTargetFramework { get; init; }

    public string SetTargetFramework { get; init; } = "";

    public required string TargetFrameworkIdentifier { get; init; }

    public required string ReferenceOutputAssembly { get; init; }

    public string IsVcxOrNativeProj { get; init; } = "";

    public required string Version { get; init; }

    public required string OutputItemType { get; init; }

    public required string TargetPlatformMonikers { get; init; }

    public required string TargetPlatformMoniker { get; init; }

    public required string ProjectReferenceOriginalItemSpec { get; init; }

    public required bool BuildReference { get; init; }

    public required string Platform { get; init; }

    public required string CopyUpToDateMarker { get; init; }

    public required string AdditionalPropertiesFromProject { get; init; }

    public required string TargetFrameworkMonikers { get; init; }

    public required string TargetFrameworks { get; init; }

    // Aliases

    public required bool IsRidAgnostic { get; init; }

    public required string MSBuildSourceTargetName { get; init; }

    public required string ResolvedFrom { get; init; }

    public required string NearestTargetFramework { get; init; }

    public required string UndefineProperties { get; init; }

    public required string OriginalProjectReferenceItemSpec { get; init; }

    public string ImageRuntime { get; init; } = ""; // Not exists on non-Windows environment.

    public required string TargetPlatformIdentifier { get; init; }

    public required string TargetFrameworkVersion { get; init; }

    public required string Platforms { get; init; }

    public required string Targets { get; init; }

    public required string OriginalItemSpec { get; init; }

    public string ReferenceAssembly { get; init; } = "";

    public required string FusionName { get; init; }


    #region Dummy properties
    // Dummy properties to avoid JSON parse error when using `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow`.
    // These metadata is not exists by default. but it's appended when `PackageId`, `VersionPrefix`, or `VersionSuffix` MSBuild properties are exists.
    public string NuGetPackageId { get; init; } = "";

    public string NuGetPackageVersion { get; init; } = "";
    #endregion
}

internal class ReferenceCopyLocalPathForUnknown : ReferenceCopyLocalPath
{
}

file sealed class BooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => bool.Parse(reader.GetString()!),
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}

file sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss.fffffff";

    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return DateTimeOffset.ParseExact(
            reader.GetString()!,
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}

file sealed class ReferenceCopyLocalPathConverter : JsonConverter<ReferenceCopyLocalPath>
{
    public override ReferenceCopyLocalPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        JsonElement root = document.RootElement;
        var json = root.GetRawText();

        if (root.TryGetProperty("AssetType", out var assetType))
        {
            var key = assetType.GetString();

            return assetType.GetString() switch
            {
                "runtime" => JsonSerializer.Deserialize<RuntimeAsset>(json, options)!,
                "resources" => JsonSerializer.Deserialize<ResourceAsset>(json, options)!,
                "native" => JsonSerializer.Deserialize<NativeAsset>(json, options)!,
                _ => throw new JsonException($"Unknown AssetType({key})")
            };
        }

        if (root.TryGetProperty("ReferenceSourceTarget", out var referenceSourceTarget))
        {
            return referenceSourceTarget.GetString() switch
            {
                "ProjectReference" => JsonSerializer.Deserialize<ProjectReference>(json, options)!,
                _ => throw new JsonException($"Unknown ReferenceSourceTarget({assetType})")
            };
        }

        throw new JsonException($"Failed to parse ReferenceCopyLocalPath. JSON: {json}");
    }

    public override void Write(Utf8JsonWriter writer, ReferenceCopyLocalPath value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}