using System.Collections.Concurrent;

namespace LumiSky.Core.Utilities;

public static class ImagingUtil
{
    public static unsafe void UInt8ToFloat(ReadOnlySpan<byte> src, Span<float> dst)
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
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(partition, parallelOptions, x =>
            {
                var source = new ReadOnlySpan<byte>(pSrc + x.Item1, x.Item2 - x.Item1);
                var target = new Span<float>(pDst + x.Item1, x.Item2 - x.Item1);
                Simd.Conversion.UInt8ToFloat(source, target);
            });
        }
    }

    public static unsafe void UInt16ToFloat(ReadOnlySpan<ushort> src, Span<float> dst)
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
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(partition, parallelOptions, x =>
            {
                var source = new ReadOnlySpan<ushort>(pSrc + x.Item1, x.Item2 - x.Item1);
                var target = new Span<float>(pDst + x.Item1, x.Item2 - x.Item1);
                Simd.Conversion.UInt16ToFloat(source, target);
            });
        }
    }

    public static unsafe void FloatToUInt8(ReadOnlySpan<float> src, Span<byte> dst)
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
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(partition, parallelOptions, x =>
            {
                var source = new ReadOnlySpan<float>(pSrc + x.Item1, x.Item2 - x.Item1);
                var target = new Span<byte>(pDst + x.Item1, x.Item2 - x.Item1);
                Simd.Conversion.FloatToUInt8(source, target);
            });
        }
    }

    public static unsafe void FloatToUInt16(ReadOnlySpan<float> src, Span<ushort> dst)
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
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(partition, parallelOptions, x =>
            {
                var source = new ReadOnlySpan<float>(pSrc + x.Item1, x.Item2 - x.Item1);
                var target = new Span<ushort>(pDst + x.Item1, x.Item2 - x.Item1);
                Simd.Conversion.FloatToUInt16(source, target);
            });
        }
    }
}