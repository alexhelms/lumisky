using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Utilities;

public static class RuntimeUtil
{
    static RuntimeUtil()
    {
        var assembly = Assembly.GetExecutingAssembly();
        if (FileVersionInfo.GetVersionInfo(assembly.Location) is { } info)
        {
            Version = info.ProductVersion ?? string.Empty;
        }
    }

    public static string Copyright { get; } = "© 2024 Alex Helms and Contributors";

    public static string Version { get; } = string.Empty;

    public static string ProcessArchitecture { get; } = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    public static string OSArchitecture { get; } = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

    public static string OSDescription { get; } = RuntimeInformation.OSDescription;

    public static string UserAgent { get; } = $"LumiSky/{Version} ({OSDescription}) ({OSArchitecture})";
}
