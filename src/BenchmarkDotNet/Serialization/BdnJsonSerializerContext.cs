using BenchmarkDotNet.Disassemblers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
)]
[JsonSerializable(typeof(ClrMdArgs))]
[JsonSerializable(typeof(Sharp))]
[JsonSerializable(typeof(MonoCode))]
[JsonSerializable(typeof(IntelAsm))]
[JsonSerializable(typeof(Arm64Asm))]
[JsonSerializable(typeof(Map))]
[JsonSerializable(typeof(DisassembledMethod))]
[JsonSerializable(typeof(DisassemblyResult))]
internal partial class BdnJsonSerializerContext : JsonSerializerContext
{
}
