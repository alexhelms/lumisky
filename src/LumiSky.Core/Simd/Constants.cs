using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Simd;

public static class Constants
{
    // Aligned for AVX/SSE, also works for ARM64.
    public const nuint AlignmentSize = 64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsAligned(void* ptr)
    {
        return ((nuint)ptr) % AlignmentSize == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsAligned<T>(ref T ptr)
        where T : unmanaged
    {
        return IsAligned(Unsafe.AsPointer(ref ptr));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsAligned<T>(Span<T> span)
        where T : unmanaged
    {
        return IsAligned(ref MemoryMarshal.GetReference(span));
    }
}
