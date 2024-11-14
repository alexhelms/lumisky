using LumiSky.Core.Memory;
using LumiSky.Core.Primitives;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Imaging;

public readonly ref struct RowInterval
{
    public RowInterval(int top, int bottom, int left, int right)
    {
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
        Width = right - left;
        Height = bottom - top;
    }

    public int Top { get; }
    public int Bottom { get; }
    public int Left { get; }
    public int Right { get; }
    public int Width { get; }
    public int Height { get; }
}

public interface IRowOperation
{
    void Invoke(int y, int left, int width);

    void Complete() { }
}

public interface IRowOperation<TBuffer>
    where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
{
    void Invoke(int y, int left, int width, Span<TBuffer> buffer);

    void Complete() { }
}

public interface IRowIntervalOperation
{
    void Invoke(in RowInterval rows);

    void Complete() { }
}

public interface IRowIntervalOperation<TBuffer>
    where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
{
    void Invoke(in RowInterval rows, Span<TBuffer> buffer);

    void Complete() { }
}

public static partial class ParallelRowIterator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DivideCeil(uint value, uint divisor) => (value + divisor - 1) / divisor;

    public const int MinPixelsPerTask = 4096;

    public static void IterateRows<TOperation>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IRowOperation
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int width = rectangle.Width;
        int height = rectangle.Height;
        int area = rectangle.Area;

        int maxSteps = (int)DivideCeil((uint)area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            try
            {
                for (int y = top; y < bottom; y++)
                {
                    Unsafe.AsRef(in operation).Invoke(y, left, width);
                }
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var rowsPerStep = (int)DivideCeil((uint)height, (uint)numSteps);
        var invoker = new RowInvoker<TOperation>(rectangle, in operation, rowsPerStep);

        try
        {
            _ = Parallel.For(
                0,
                numSteps,
                new ParallelOptions { MaxDegreeOfParallelism = numSteps },
                invoker.Invoke);
        }
        finally
        {
            Unsafe.AsRef(in operation).Complete();
        }
    }

    public static void IterateRows<TOperation, TBuffer>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IRowOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int width = rectangle.Width;
        int height = rectangle.Height;
        int area = rectangle.Area;

        int maxSteps = (int)DivideCeil((uint)area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(rectangle.Area);

            try
            {
                for (int y = top; y < bottom; y++)
                {
                    Unsafe.AsRef(in operation).Invoke(y, left, width, buffer.Memory.Span);
                }
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var rowsPerStep = (int)DivideCeil((uint)height, (uint)numSteps);
        var invoker = new RowInvokerWithBuffer<TOperation, TBuffer>(rectangle, in operation, rowsPerStep);

        try
        {
            _ = Parallel.For(
                0,
                numSteps,
                new ParallelOptions { MaxDegreeOfParallelism = numSteps },
                invoker.Invoke);
        }
        finally
        {
            Unsafe.AsRef(in operation).Complete();
        }
    }

    public static void IterateRowIntervals<TOperation>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IRowIntervalOperation
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int right = rectangle.Right;
        int height = rectangle.Height;
        int area = rectangle.Area;

        int maxSteps = (int)DivideCeil((uint)area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            var rows = new RowInterval(top, bottom, left, right);
            try
            {
                Unsafe.AsRef(in operation).Invoke(in rows);
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var rowsPerStep = (int)DivideCeil((uint)height, (uint)numSteps);
        var invoker = new RowIntervalInvoker<TOperation>(rectangle, in operation, rowsPerStep);

        try
        {
            _ = Parallel.For(
                0,
                numSteps,
                new ParallelOptions { MaxDegreeOfParallelism = numSteps },
                invoker.Invoke);
        }
        finally
        {
            Unsafe.AsRef(in operation).Complete();
        }
    }

    public static void IterateRowIntervals<TOperation, TBuffer>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IRowIntervalOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int right = rectangle.Right;
        int height = rectangle.Height;
        int area = rectangle.Area;

        int maxSteps = (int)DivideCeil((uint) area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            var rows = new RowInterval(top, bottom, left, right);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(rectangle.Area);
            try
            {
                Unsafe.AsRef(in operation).Invoke(in rows, buffer.Memory.Span);
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var rowsPerStep = (int)DivideCeil((uint)height, (uint)numSteps);
        var invoker = new RowIntervalWithBufferInvoker<TOperation, TBuffer>(rectangle, in operation, rowsPerStep);

        try
        {
            _ = Parallel.For(
            0,
            numSteps,
            new ParallelOptions { MaxDegreeOfParallelism = numSteps },
            invoker.Invoke);
        }
        finally
        {
            Unsafe.AsRef(in operation).Complete();
        }
    }
}
