using System.Runtime.CompilerServices;

namespace LumiSky.Core.Utilities;

public static class Util
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    public static string ExtensionToMimeType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream",
        };
}