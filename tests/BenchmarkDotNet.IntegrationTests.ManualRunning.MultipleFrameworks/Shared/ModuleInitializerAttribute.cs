#if NETFRAMEWORK
namespace System.Runtime.CompilerServices;

#pragma warning disable CA1018 // CA1018: Mark attributes with AttributeUsageAttribute

// See: https://github.com/thomhurst/TUnit/issues/3731
public sealed class ModuleInitializerAttribute : Attribute 
{
}
#endif
