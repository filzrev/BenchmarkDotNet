using BenchmarkDotNet.Environments;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BdnRuntimeInformation = BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Tests.XUnit;

public static class EnvRequirementChecker
{
    public static string? GetSkip(params EnvRequirement[] requirements) => requirements.Select(GetSkip).FirstOrDefault(skip => skip != null);

    internal static string? GetSkip(EnvRequirement requirement) => requirement switch
    {
        EnvRequirement.WindowsOnly => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Windows-only test",
        EnvRequirement.NonWindows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : "Non-Windows test",
        EnvRequirement.NonWindowsArm => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !IsArm() || IsArm() && IsRunningAsX64EmulatedOnArm64() ? null : "Non-Windows+Arm test",
        EnvRequirement.NonLinux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? null : "Non-Linux test",
        EnvRequirement.FullFrameworkOnly => BdnRuntimeInformation.IsFullFramework ? null : "Full .NET Framework-only test",
        EnvRequirement.NonFullFramework => !BdnRuntimeInformation.IsFullFramework ? null : "Non-Full .NET Framework test",
        EnvRequirement.DotNetCoreOnly => BdnRuntimeInformation.IsNetCore ? null : ".NET/.NET Core-only test",
        EnvRequirement.NeedsPrivilegedProcess => IsPrivilegedProcess() ? null : "Needs authorization to perform security-relevant functions",
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unknown value")
    };

    private static bool IsPrivilegedProcess()
    {
#if NET462
        using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(currentUser).IsInRole(WindowsBuiltInRole.Administrator);
#else
        return Environment.IsPrivilegedProcess;
#endif
    }

    private static bool IsArm()
        => BdnRuntimeInformation.GetCurrentPlatform() is Platform.Arm64 or Platform.Arm or Platform.Armv6;

    private static bool IsRunningAsX64EmulatedOnArm64()
    {
        if (!IsWow64Process2(
            System.Diagnostics.Process.GetCurrentProcess().Handle,
            out ushort processMachine,
            out ushort nativeMachine))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return nativeMachine == IMAGE_FILE_MACHINE_ARM64 &&
               processMachine == IMAGE_FILE_MACHINE_AMD64;
    }

    private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
    private const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(
        IntPtr hProcess,
        out ushort processMachine,
        out ushort nativeMachine);
}