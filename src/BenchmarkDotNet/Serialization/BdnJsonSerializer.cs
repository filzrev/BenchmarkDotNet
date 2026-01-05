using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BenchmarkDotNet.Serialization;

public static partial class BdnJsonSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = BdnJsonSerializerContext.Default,
    };

    public static string Serialize<T>(T item, bool indentJson = false)
    {
        if (indentJson)
            return JsonSerializer.Serialize(item, IndentedOptions);

        var jsonTypeInfo = BdnJsonSerializerContext.Default.GetTypeInfo(typeof(T));
        if (jsonTypeInfo == null)
            throw new NotSupportedException($"Type({typeof(T).Name}) is not supported.");

        return JsonSerializer.Serialize(item, jsonTypeInfo);
    }

    public static T Deserialize<T>(string json)
    {
        var jsonTypeInfo = (JsonTypeInfo<T>?)BdnJsonSerializerContext.Default.GetTypeInfo(typeof(T));
        if (jsonTypeInfo == null)
            throw new NotSupportedException($"Type({typeof(T).Name}) is not supported.");

        return JsonSerializer.Deserialize(json, jsonTypeInfo);
    }
}
