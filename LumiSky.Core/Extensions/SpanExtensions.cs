using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

public static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference<T>(this Span<T> span)
    {
        return ref MemoryMarshal.GetReference(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference<T>(this ReadOnlySpan<T> span)
    {
        return ref MemoryMarshal.GetReference(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(this Span<T> span, int i)
    {
        ref T r0 = ref MemoryMarshal.GetReference(span);
        return ref Unsafe.Add(ref r0, (nint)(uint)i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(this Span<T> span, nint i)
    {
        ref T r0 = ref MemoryMarshal.GetReference(span);
        return ref Unsafe.Add(ref r0, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(this ReadOnlySpan<T> span, int i)
    {
        ref T r0 = ref MemoryMarshal.GetReference(span);
        return ref Unsafe.Add(ref r0, (nint)(uint)i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(this ReadOnlySpan<T> span, nint i)
    {
        ref T r0 = ref MemoryMarshal.GetReference(span);
        return ref Unsafe.Add(ref r0, i);
    }

    public unsafe static string NullTerminatedToString(this Span<byte> span)
    {
        fixed (byte* b = &MemoryMarshal.GetReference(span))
        {
            var stringSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(b);
            return Encoding.UTF8.GetString(stringSpan);
        }
    }

    public unsafe static string NullTerminatedToString(this ReadOnlySpan<byte> span)
    {
        fixed (byte* b = &MemoryMarshal.GetReference(span))
        {
            var stringSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(b);
            return Encoding.UTF8.GetString(stringSpan);
        }
    }
}
