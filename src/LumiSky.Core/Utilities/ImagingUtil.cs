﻿using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LumiSky.Core.Utilities;

public static class ImagingUtil
{
    public static unsafe void UInt8ToNormalizedFloat(ReadOnlySpan<byte> src, Span<float> dst, bool avx = true)
    {
        if (src.Length != dst.Length) throw new ArgumentException("src and dst must be equal length");
        if (src.Length == 0) return;

        fixed (byte* pS = src)
        fixed (float* pD = dst)
        {
            // Need local copy
            byte* pSrc = pS;
            float* pDst = pD;

            var partition = Partitioner.Create(0, src.Length);

            if (avx && Avx.IsSupported && Avx2.IsSupported)
            {
                if (src.Length > 1_000_000)
                {
                    Parallel.ForEach(
                        partition,
                        new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        x =>
                        {
                            var source = new Span<byte>(pSrc + x.Item1, x.Item2 - x.Item1);
                            var target = new Span<float>(pDst + x.Item1, x.Item2 - x.Item1);
                            Simd.UInt8ToFloat(source, target);
                        });
                }
                else
                {
                    Simd.UInt8ToFloat(src, dst);
                }
            }
            else
            {
                Parallel.ForEach(
                    partition,
                    new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    x =>
                    {
                        ref var source = ref MemoryMarshal.GetReference(new Span<byte>(pSrc + x.Item1, x.Item2 - x.Item1));
                        ref var target = ref MemoryMarshal.GetReference(new Span<float>(pDst + x.Item1, x.Item2 - x.Item1));

                        int length= x.Item2 - x.Item1;
                        for (int i = 0; i < length; i++)
                            Unsafe.Add(ref target, i) = Unsafe.Add(ref source, i) * (1.0f / byte.MaxValue);
                    });
            }
        }
    }

    public static unsafe void UInt16ToNormalizedFloat(ReadOnlySpan<ushort> src, Span<float> dst, bool avx = true)
    {
        if (src.Length != dst.Length) throw new ArgumentException("src and dst must be equal length");
        if (src.Length == 0) return;

        fixed (ushort* pS = src)
        fixed (float* pD = dst)
        {
            // Need local copy
            ushort* pSrc = pS;
            float* pDst = pD;

            var partition = Partitioner.Create(0, src.Length);

            if (avx && Avx.IsSupported && Avx2.IsSupported)
            {
                if (src.Length > 1_000_000)
                {
                    Parallel.ForEach(
                        partition,
                        new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        x =>
                        {
                            var source = new Span<ushort>(pSrc + x.Item1, x.Item2 - x.Item1);
                            var target = new Span<float>(pDst + x.Item1, x.Item2 - x.Item1);
                            Simd.UInt16ToFloat(source, target);
                        });
                }
                else
                {
                    Simd.UInt16ToFloat(src, dst);
                }
            }
            else
            {
                Parallel.ForEach(
                    partition,
                    new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    x =>
                    {
                        ref var source = ref MemoryMarshal.GetReference(new Span<ushort>(pSrc + x.Item1, x.Item2 - x.Item1));
                        ref var target = ref MemoryMarshal.GetReference(new Span<float>(pDst + x.Item1, x.Item2 - x.Item1));

                        int length= x.Item2 - x.Item1;
                        for (int i = 0; i < length; i++)
                            Unsafe.Add(ref target, i) = Unsafe.Add(ref source, i) * (1.0f / ushort.MaxValue);
                    });
            }
        }
    }

    public static unsafe void NormalizedFloatToUInt8(ReadOnlySpan<float> src, Span<byte> dst, bool avx = true)
    {
        if (src.Length != dst.Length) throw new ArgumentException("src and dst must be equal length");
        if (src.Length == 0) return;

        fixed (float* pS = src)
        fixed (byte* pD = dst)
        {
            // Need local copy
            float* pSrc = pS;
            byte* pDst = pD;

            var partition = Partitioner.Create(0, src.Length);

            if (avx && Avx.IsSupported && Avx2.IsSupported)
            {
                if (src.Length > 1_000_000)
                {
                    Parallel.ForEach(
                        partition,
                        x =>
                        {
                            var source = new Span<float>(pSrc + x.Item1, x.Item2 - x.Item1);
                            var target = new Span<byte>(pDst + x.Item1, x.Item2 - x.Item1);
                            Simd.FloatToUInt8(source, target);
                        });
                }
                else
                {
                    Simd.FloatToUInt8(src, dst);
                }
            }
            else
            {
                Parallel.ForEach(
                    partition,
                    x =>
                    {
                        ref var source = ref MemoryMarshal.GetReference(new Span<float>(pSrc + x.Item1, x.Item2 - x.Item1));
                        ref var target = ref MemoryMarshal.GetReference(new Span<byte>(pDst + x.Item1, x.Item2 - x.Item1));

                        int length= x.Item2 - x.Item1;
                        for (int i = 0; i < length; i++)
                            Unsafe.Add(ref target, i) = (byte)(Unsafe.Add(ref source, i) * byte.MaxValue);
                    });
            }
        }
    }

    public static unsafe void NormalizedFloatToUInt16(ReadOnlySpan<float> src, Span<ushort> dst, bool avx = true)
    {
        if (src.Length != dst.Length) throw new ArgumentException("src and dst must be equal length");
        if (src.Length == 0) return;

        fixed (float* pS = src)
        fixed (ushort* pD = dst)
        {
            // Need local copy
            float* pSrc = pS;
            ushort* pDst = pD;

            var partition = Partitioner.Create(0, src.Length);

            if (avx && Avx.IsSupported && Avx2.IsSupported)
            {
                if (src.Length > 1_000_000)
                {
                    Parallel.ForEach(
                        partition,
                        x =>
                        {
                            var source = new Span<float>(pSrc + x.Item1, x.Item2 - x.Item1);
                            var target = new Span<ushort>(pDst + x.Item1, x.Item2 - x.Item1);
                            Simd.FloatToUInt16(source, target);
                        });
                }
                else
                {
                    Simd.FloatToUInt16(src, dst);
                }
            }
            else
            {
                Parallel.ForEach(
                    partition,
                    x =>
                    {
                        ref var source = ref MemoryMarshal.GetReference(new Span<float>(pSrc + x.Item1, x.Item2 - x.Item1));
                        ref var target = ref MemoryMarshal.GetReference(new Span<ushort>(pDst + x.Item1, x.Item2 - x.Item1));

                        int length= x.Item2 - x.Item1;
                        for (int i = 0; i < length; i++)
                            Unsafe.Add(ref target, i) = (ushort)(Unsafe.Add(ref source, i) * ushort.MaxValue);
                    });
            }
        }
    }
}