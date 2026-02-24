#! dotnet
#:property PublishAot = false
#:property LangVersion = Latest
#:property PreferNativeArm64 = true

using System;
using System.Runtime.InteropServices;

// .NET Runtime information
Console.WriteLine(RuntimeInformation.FrameworkDescription);
Console.WriteLine(RuntimeInformation.OSDescription);

Console.WriteLine();

// Environment information
Console.WriteLine($"{nameof(RuntimeInformation.OSArchitecture)}: {RuntimeInformation.OSArchitecture}");
Console.WriteLine($"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}");
