using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace LumiSky.Core.Simd;

public static class Conversion
{
    public static void FloatToUInt8(ReadOnlySpan<float> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);

        if (Vector.IsHardwareAccelerated)
        {
            FloatToUInt8_Vector(input, output);
        }
        else
        {
            FloatToUInt8_Scalar(input, output);
        }
    }

    private static void FloatToUInt8_Scalar(ReadOnlySpan<float> input, Span<byte> output)
    {
        const ushort scale = byte.MaxValue;

        ref float inputRef = ref MemoryMarshal.GetReference(input);
        ref byte outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint length = (nuint)input.Length;

        for (; elementOffset < length; elementOffset++)
        {
            Unsafe.Add(ref outputRef, elementOffset) = (byte)(Unsafe.Add(ref inputRef, elementOffset) * scale);
        }
    }

    private static void FloatToUInt8_Vector(ReadOnlySpan<float> input, Span<byte> output)
    {
        ref float inputRef = ref MemoryMarshal.GetReference(input);
        ref byte outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint oneVectorAwayFromEnd = (nuint)(output.Length - Vector<byte>.Count);

        const byte scale = byte.MaxValue;
        Vector<float> vScale = new(scale);

        if (input.Length >= Vector<ushort>.Count)
        {
            for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector<byte>.Count)
            {
                // Load float32 vectors
                Vector<float> floats1 = Vector.LoadUnsafe(ref inputRef, elementOffset);
                Vector<float> floats2 = Vector.LoadUnsafe(ref inputRef, elementOffset + (nuint)Vector<float>.Count);
                Vector<float> floats3 = Vector.LoadUnsafe(ref inputRef, elementOffset + (nuint)Vector<float>.Count * 2);
                Vector<float> floats4 = Vector.LoadUnsafe(ref inputRef, elementOffset + (nuint)Vector<float>.Count * 3);

                // Scale and convert to uint
                Vector<uint> uints1 = Vector.ConvertToUInt32Native(floats1 * vScale);
                Vector<uint> uints2 = Vector.ConvertToUInt32Native(floats2 * vScale);
                Vector<uint> uints3 = Vector.ConvertToUInt32Native(floats3 * vScale);
                Vector<uint> uints4 = Vector.ConvertToUInt32Native(floats4 * vScale);

                // Narrow the uint32 to uint16 vectors
                Vector<ushort> ushorts1 = Vector.Narrow(uints1, uints2);
                Vector<ushort> ushorts2 = Vector.Narrow(uints3, uints4);

                // Narrow to bytes
                Vector<byte> bytes = Vector.Narrow(ushorts1, ushorts2);

                bytes.StoreUnsafe(ref outputRef, elementOffset);
            }
        }

        // Remainder
        while (elementOffset < (nuint)input.Length)
        {
            Unsafe.Add(ref outputRef, elementOffset) = (byte)(Unsafe.Add(ref inputRef, elementOffset) * scale);
            elementOffset++;
        }
    }

    public static void FloatToUInt16(ReadOnlySpan<float> input, Span<ushort> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);

        if (Vector.IsHardwareAccelerated)
        {
            FloatToUInt16_Vector(input, output);
        }
        else
        {
            FloatToUInt16_Scalar(input, output);
        }
    }

    private static void FloatToUInt16_Scalar(ReadOnlySpan<float> input, Span<ushort> output)
    {
        const ushort scale = ushort.MaxValue;

        ref float inputRef = ref MemoryMarshal.GetReference(input);
        ref ushort outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint length = (nuint)input.Length;

        for (; elementOffset < length; elementOffset++)
        {
            Unsafe.Add(ref outputRef, elementOffset) = (ushort)(Unsafe.Add(ref inputRef, elementOffset) * scale);
        }
    }

    private static void FloatToUInt16_Vector(ReadOnlySpan<float> input, Span<ushort> output)
    {
        ref float inputRef = ref MemoryMarshal.GetReference(input);
        ref ushort outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint oneVectorAwayFromEnd = (nuint)(output.Length - Vector<ushort>.Count);

        const ushort scale = ushort.MaxValue;
        Vector<float> vScale = new(scale);

        if (input.Length >= Vector<ushort>.Count)
        {
            for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector<ushort>.Count)
            {
                // Load float32 vectors
                Vector<float> asFloat_1 = Vector.LoadUnsafe(ref inputRef, elementOffset);
                Vector<float> asFloat_2 = Vector.LoadUnsafe(ref inputRef, elementOffset + (nuint)Vector<float>.Count);

                // Scale and convert to uint
                Vector<uint> asUint32_1 = Vector.ConvertToUInt32Native(asFloat_1 * vScale);
                Vector<uint> asUint32_2 = Vector.ConvertToUInt32Native(asFloat_2 * vScale);

                // Narrow the uint32 to uint16 vectors
                Vector<ushort> asUint16 = Vector.Narrow(asUint32_1, asUint32_2);

                asUint16.StoreUnsafe(ref outputRef, elementOffset);
            }
        }

        // Remainder
        while (elementOffset < (nuint)input.Length)
        {
            Unsafe.Add(ref outputRef, elementOffset) = (ushort)(Unsafe.Add(ref inputRef, elementOffset) * scale);
            elementOffset++;
        }
    }

    public static void UInt8ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);

        if (Vector.IsHardwareAccelerated)
        {
            UInt8ToFloat_Vector(input, output);
        }
        else
        {
            UInt8ToFloat_Scalar(input, output);
        }
    }

    private static void UInt8ToFloat_Scalar(ReadOnlySpan<byte> input, Span<float> output)
    {
        const float scale = 1.0f / byte.MaxValue;

        ref byte inputRef = ref MemoryMarshal.GetReference(input);
        ref float outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint length = (nuint)input.Length;

        for (; elementOffset < length; elementOffset++)
        {
            Unsafe.Add(ref outputRef, elementOffset) = Unsafe.Add(ref inputRef, elementOffset) * scale;
        }
    }

    private static void UInt8ToFloat_Vector(ReadOnlySpan<byte> input, Span<float> output)
    {
        ref byte inputRef = ref MemoryMarshal.GetReference(input);
        ref float outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint oneVectorAwayFromEnd = (nuint)(output.Length - Vector<byte>.Count);

        const float scale = 1.0f / byte.MaxValue;
        Vector<float> vScale = new(scale);

        if (input.Length >= Vector<byte>.Count)
        {
            for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector<byte>.Count)
            {
                // Load uint8 vector
                Vector<byte> loaded = Vector.LoadUnsafe(ref inputRef, elementOffset);

                // Widen byte to ushort (first widening)
                Vector.Widen(loaded, out var ushortLo, out var ushortHi);

                // Widen ushort to uint (second widening)
                Vector.Widen(ushortLo, out var uintLoLo, out var uintLoHi);
                Vector.Widen(ushortHi, out var uintHiLo, out var uintHiHi);

                // Convert to float32 and normalize [0..1]
                var normalizedLoLo = Vector.ConvertToSingle(uintLoLo) * vScale;
                var normalizedLoHi = Vector.ConvertToSingle(uintLoHi) * vScale;
                var normalizedHiLo = Vector.ConvertToSingle(uintHiLo) * vScale;
                var normalizedHiHi = Vector.ConvertToSingle(uintHiHi) * vScale;

                // Store the floats
                nuint floatOffset = elementOffset;
                Vector.StoreUnsafe(normalizedLoLo, ref outputRef, floatOffset);
                floatOffset += (nuint)Vector<float>.Count;
                Vector.StoreUnsafe(normalizedLoHi, ref outputRef, floatOffset);
                floatOffset += (nuint)Vector<float>.Count;
                Vector.StoreUnsafe(normalizedHiLo, ref outputRef, floatOffset);
                floatOffset += (nuint)Vector<float>.Count;
                Vector.StoreUnsafe(normalizedHiHi, ref outputRef, floatOffset);
            }
        }

        // Remainder
        while (elementOffset < (nuint)input.Length)
        {
            Unsafe.Add(ref outputRef, elementOffset) = Unsafe.Add(ref inputRef, elementOffset) * scale;
            elementOffset++;
        }
    }

    public static void UInt16ToFloat(ReadOnlySpan<ushort> input, Span<float> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);

        if (Vector.IsHardwareAccelerated)
        {
            UInt16ToFloat_Vector(input, output);
        }
        else
        {
            UInt16ToFloat_Scalar(input, output);
        }
    }

    private static void UInt16ToFloat_Scalar(ReadOnlySpan<ushort> input, Span<float> output)
    {
        const float scale = 1.0f / ushort.MaxValue;

        ref ushort inputRef = ref MemoryMarshal.GetReference(input);
        ref float outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint length = (nuint)input.Length;

        for (; elementOffset < length; elementOffset++)
        {
            Unsafe.Add(ref outputRef, elementOffset) = Unsafe.Add(ref inputRef, elementOffset) * scale;
        }
    }

    private static void UInt16ToFloat_Vector(ReadOnlySpan<ushort> input, Span<float> output)
    {
        ref ushort inputRef = ref MemoryMarshal.GetReference(input);
        ref float outputRef = ref MemoryMarshal.GetReference(output);
        nuint elementOffset = 0;
        nuint oneVectorAwayFromEnd = (nuint)(output.Length - Vector<ushort>.Count);

        const float scale = 1.0f / ushort.MaxValue;
        Vector<float> vScale = new(scale);

        if (input.Length >= Vector<ushort>.Count)
        {
            for (; elementOffset <= oneVectorAwayFromEnd; elementOffset += (nuint)Vector<ushort>.Count)
            {
                // Load uint16 vector
                Vector<ushort> loaded = Vector.LoadUnsafe(ref inputRef, elementOffset);

                // Widen to two int32 vectors
                Vector.Widen(loaded, out var int32Lo, out var int32Hi);

                // Convert to float32 and normalize [0..1]
                var normalizedLo = Vector.ConvertToSingle(int32Lo) * vScale;
                var normalizedHi = Vector.ConvertToSingle(int32Hi) * vScale;

                // Store the 8 floats
                Vector.StoreUnsafe(normalizedLo, ref outputRef, elementOffset);
                Vector.StoreUnsafe(normalizedHi, ref outputRef, elementOffset + (nuint)(Vector<ushort>.Count / 2));
            }
        }

        // Remainder
        while (elementOffset < (nuint)input.Length)
        {
            Unsafe.Add(ref outputRef, elementOffset) = Unsafe.Add(ref inputRef, elementOffset) * scale;
            elementOffset++;
        }
    }
}
