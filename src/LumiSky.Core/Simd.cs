using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace LumiSky.Core;

public static class Simd
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

    public static unsafe void FloatToUInt8Avx2(ReadOnlySpan<float> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!Avx2.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 256-bit registers
        const int ElementsPerVector = 8;
        const int ElementsPerBatch = 4 * ElementsPerVector;

        fixed (float* pInput = input)
        fixed (byte* pOutput = output)
        {
            float* pSrc = pInput;
            float* pEnd = pSrc + input.Length;
            byte* pDst = pOutput;

            Vector256<float> vScale = Vector256.Create((float)byte.MaxValue);

            while (pSrc < pEnd - ElementsPerBatch)
            {
                Vector256<int> a = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 0 * ElementsPerVector) * vScale);
                Vector256<int> b = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 1 * ElementsPerVector) * vScale);
                Vector256<int> c = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 2 * ElementsPerVector) * vScale);
                Vector256<int> d = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 3 * ElementsPerVector) * vScale);
                Vector256<short> ab = Avx2.PackSignedSaturate(a, b);
                Vector256<short> cd = Avx2.PackSignedSaturate(c, d);
                Vector256<byte> abcd = Avx2.PackUnsignedSaturate(ab, cd);

                // Packed to one vector, but in [ a_lo, b_lo, c_lo, d_lo | a_hi, b_hi, c_hi, d_hi ] order.
                // Unpack it so it's sequential.
                Vector256<int> fix = Avx2.PermuteVar8x32(abcd.AsInt32(), Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7));
                Avx.Store(pDst, fix.AsByte());

                pSrc += ElementsPerBatch;
                pDst += ElementsPerBatch;
            }

            while (pSrc < pEnd)
            {
                *pDst = (byte)(*pSrc * byte.MaxValue);
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void FloatToUInt8Arm(ReadOnlySpan<float> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!AdvSimd.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 128-bit registers
        const int ElementsPerVector = 4;
        const int ElementsPerBatch = 1 * ElementsPerVector;

        fixed (float* pInput = input)
        fixed (byte* pOutput = output)
        {
            float* pSrc = pInput;
            float* pEnd = pSrc + input.Length;
            byte* pDst = pOutput;

            Vector128<float> scale = Vector128.Create((float)byte.MaxValue);

            while (pSrc < pEnd - ElementsPerBatch)
            {
                Vector128<float> f32 = AdvSimd.LoadVector128(pSrc) * scale;
                Vector128<uint> u32 = AdvSimd.ConvertToUInt32RoundToZero(f32);
                Vector64<ushort> u16 = AdvSimd.ExtractNarrowingSaturateLower(u32);
                Vector64<byte> u8 = AdvSimd.ExtractNarrowingSaturateLower(Vector128.Create(u16, u16));
                AdvSimd.StoreSelectedScalar(pDst + 0, u8, 0);
                AdvSimd.StoreSelectedScalar(pDst + 1, u8, 1);
                AdvSimd.StoreSelectedScalar(pDst + 2, u8, 2);
                AdvSimd.StoreSelectedScalar(pDst + 3, u8, 3);

                pSrc += ElementsPerBatch;
                pDst += ElementsPerBatch;
            }

            while (pSrc < pEnd)
            {
                *pDst = (byte)(*pSrc * byte.MaxValue);
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void FloatToUInt16Avx2(ReadOnlySpan<float> input, Span<ushort> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!Avx2.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 256-bit registers
        const int ElementsPerVector = 8;
        const int ElementsPerBatch = 2 * ElementsPerVector;

        fixed (float* pInput = input)
        fixed (ushort* pOutput = output)
        {
            float* pSrc = pInput;
            float* pEnd = pSrc + input.Length;
            ushort* pDst = pOutput;

            Vector256<float> vScale = Vector256.Create((float)ushort.MaxValue);

            while (pSrc < pEnd - ElementsPerBatch)
            {
                Vector256<int> a = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 0 * ElementsPerVector) * vScale);
                Vector256<int> b = Avx.ConvertToVector256Int32(Avx.LoadVector256(pSrc + 1 * ElementsPerVector) * vScale);
                Vector256<ushort> ab = Avx2.PackUnsignedSaturate(a, b);

                // Packed to one vector, but in [ a_lo, b_lo | a_hi, b_hi ] order.
                // Unpack it so it's sequential.
                Vector256<ulong> fix = Avx2.Permute4x64(ab.AsUInt64(), 0xD8);
                Avx.Store(pDst, fix.AsUInt16());

                pSrc += ElementsPerBatch;
                pDst += ElementsPerBatch;
            }

            while (pSrc < pEnd)
            {
                *pDst = (ushort)(*pSrc * ushort.MaxValue);
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void FloatToUInt16Arm(ReadOnlySpan<float> input, Span<ushort> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!AdvSimd.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 128-bit registers
        const int ElementsPerVector = 4;
        const int ElementsPerBatch = 1 * ElementsPerVector;

        fixed (float* pInput = input)
        fixed (ushort* pOutput = output)
        {
            float* pSrc = pInput;
            float* pEnd = pSrc + input.Length;
            ushort* pDst = pOutput;

            Vector128<float> scale = Vector128.Create((float)ushort.MaxValue);

            while (pSrc < pEnd - ElementsPerBatch)
            {
                Vector128<float> f32 = AdvSimd.LoadVector128(pSrc) * scale;
                Vector128<uint> u32 = AdvSimd.ConvertToUInt32RoundToZero(f32);
                Vector64<ushort> u16 = AdvSimd.ExtractNarrowingSaturateLower(u32);
                AdvSimd.Store(pDst, u16);

                pSrc += ElementsPerBatch;
                pDst += ElementsPerBatch;
            }

            while (pSrc < pEnd)
            {
                *pDst = (ushort)(*pSrc * ushort.MaxValue);
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void UInt8ToFloatAvx2(ReadOnlySpan<byte> input, Span<float> output)
    {
        if (input.Length != output.Length) throw new ArgumentException("input and output length must be equal");
        if (!Avx.IsSupported || !Avx2.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        const int ElementsPerVector = 8;
        const int ElementsPerBatch = 4 * ElementsPerVector;

        fixed (byte* pInput = input)
        fixed (float* pOutput = output)
        {
            byte* pSrc = pInput;
            byte* pEnd = pSrc + input.Length;
            float* pDst = pOutput;

            Vector256<float> vScale = Vector256.Create(1.0f / byte.MaxValue);

            while (pSrc < pEnd - ElementsPerBatch)
            {
                Vector256<byte> v = Avx.LoadVector256(pSrc);

                Vector256<byte> alo = Avx2.UnpackLow(v, Vector256<byte>.Zero);
                Vector256<byte> ahi = Avx2.UnpackHigh(v, Vector256<byte>.Zero);

                Vector256<ushort> b1 = Avx2.UnpackLow(alo.AsUInt16(), Vector256<ushort>.Zero);
                Vector256<ushort> b2 = Avx2.UnpackHigh(alo.AsUInt16(), Vector256<ushort>.Zero);
                Vector256<ushort> b3 = Avx2.UnpackLow(ahi.AsUInt16(), Vector256<ushort>.Zero);
                Vector256<ushort> b4 = Avx2.UnpackHigh(ahi.AsUInt16(), Vector256<ushort>.Zero);

                Vector256<float> c1 = Avx.ConvertToVector256Single(b1.AsInt32());
                Vector256<float> c2 = Avx.ConvertToVector256Single(b2.AsInt32());
                Vector256<float> c3 = Avx.ConvertToVector256Single(b3.AsInt32());
                Vector256<float> c4 = Avx.ConvertToVector256Single(b4.AsInt32());

                // Rearrange the vector to be sequential, followed by normalization
                Avx.Store(pDst + 0 * ElementsPerVector, Avx.Permute2x128(c1, c2, 0x20) * vScale);
                Avx.Store(pDst + 1 * ElementsPerVector, Avx.Permute2x128(c3, c4, 0x20) * vScale);
                Avx.Store(pDst + 2 * ElementsPerVector, Avx.Permute2x128(c1, c2, 0x31) * vScale);
                Avx.Store(pDst + 3 * ElementsPerVector, Avx.Permute2x128(c3, c4, 0x31) * vScale);

                pSrc += ElementsPerBatch;
                pDst += ElementsPerBatch;
            }

            while (pSrc < pEnd)
            {
                *pDst = (float)(*pSrc * (1.0f / byte.MaxValue));
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void UInt16ToFloatAvx2(ReadOnlySpan<ushort> input, Span<float> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!Avx2.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 256-bit registers
        const int ElementsPerVector = 8;

        fixed (ushort* pInput = input)
        fixed (float* pOutput = output)
        {
            ushort* pSrc = pInput;
            ushort* pEnd = pSrc + input.Length;
            float* pDst = pOutput;

            Vector256<float> vScale = Vector256.Create(1.0f / ushort.MaxValue);

            while (pSrc < pEnd - ElementsPerVector)
            {
                Vector256<int> asInt32 = Avx2.ConvertToVector256Int32(pSrc);
                Vector256<float> asFloat = Avx.ConvertToVector256Single(asInt32);
                Vector256<float> normalized = Avx.Multiply(asFloat, vScale);
                Avx.Store(pDst, normalized);

                pSrc += ElementsPerVector;
                pDst += ElementsPerVector;
            }

            while (pSrc < pEnd)
            {
                *pDst = (float)(*pSrc * (1.0f / ushort.MaxValue));
                pSrc++;
                pDst++;
            }
        }
    }

    public static unsafe void UInt16ToFloatArm(ReadOnlySpan<ushort> input, Span<float> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(input.Length, output.Length);
        if (!AdvSimd.IsSupported) throw new NotSupportedException();
        if (input.Length == 0) return;

        // 128-bit registers
        const int ElementsPerVector = 4;

        fixed (ushort* pInput = input)
        fixed (float* pOutput = output)
        {
            ushort* pSrc = pInput;
            ushort* pEnd = pSrc + input.Length;
            float* pDst = pOutput;

            Vector128<float> scale = Vector128.Create(1.0f / ushort.MaxValue);

            while (pSrc < pEnd - ElementsPerVector)
            {
                Vector128<uint> asUInt32 = AdvSimd.ZeroExtendWideningLower(Vector64.Load(pSrc));
                Vector128<float> asFloat = AdvSimd.ConvertToSingle(asUInt32);
                Vector128<float> normalized = AdvSimd.Multiply(asFloat, scale);
                AdvSimd.Store(pDst, normalized);

                pSrc += ElementsPerVector;
                pDst += ElementsPerVector;
            }

            while (pSrc < pEnd)
            {
                *pDst = (float)(*pSrc * (1.0f / ushort.MaxValue));
                pSrc++;
                pDst++;
            }
        }
    }
}
