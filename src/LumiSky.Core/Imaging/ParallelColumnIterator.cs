using LumiSky.Core.Memory;
using LumiSky.Core.Primitives;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Imaging;

public readonly ref struct ColumnInterval
{
    public ColumnInterval(int top, int bottom, int left, int right)
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

public interface IColumnOperation
{
    void Invoke(int x, int top, int bottom);

    void Complete() { }
}

public interface IColumnOperation<TBuffer>
    where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
{
    void Invoke(int x, int top, int bottom, Span<TBuffer> buffer);

    void Complete() { }
}

public interface IColumnIntervalOperation
{
    void Invoke(in ColumnInterval cols);

    void Complete() { }
}

public interface IColumnIntervalOperation<TBuffer>
    where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
{
    void Invoke(in ColumnInterval cols, Span<TBuffer> buffer);

    void Complete() { }
}

public static partial class ParallelColumnIterator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DivideCeil(int dividend, int divisor) => 1 + ((dividend - 1) / divisor);

    public const int MinPixelsPerTask = 4096;

    public static void IterateColumns<TOperation>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IColumnOperation
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int width = rectangle.Width;
        int right = rectangle.Right;
        int area = rectangle.Area;

        int maxSteps = DivideCeil(area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            try
            {
                for (int x = left; x < right; x++)
                {
                    Unsafe.AsRef(in operation).Invoke(x, top, bottom);
                }
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var colsPerStep = DivideCeil(width, numSteps);
        var invoker = new ColumnInvoker<TOperation>(rectangle, in operation, colsPerStep);

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

    public static void IterateColumns<TOperation, TBuffer>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IColumnOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int right = rectangle.Right;
        int width = rectangle.Width;
        int area = rectangle.Area;

        int maxSteps = DivideCeil(area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(rectangle.Area);

            try
            {
                for (int x = left; x < right; x++)
                {
                    Unsafe.AsRef(in operation).Invoke(x, top, bottom, buffer.Memory.Span);
                }
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var colsPerStep = DivideCeil(width, numSteps);
        var invoker = new ColumnInvokerWithBuffer<TOperation, TBuffer>(rectangle, in operation, colsPerStep);

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

    public static void IterateColumnIntervals<TOperation>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IColumnIntervalOperation
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int right = rectangle.Right;
        int width = rectangle.Width;
        int area = rectangle.Area;

        int maxSteps = DivideCeil(area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            var cols = new ColumnInterval(top, bottom, left, right);
            try
            {
                Unsafe.AsRef(in operation).Invoke(in cols);
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var colsPerStep = DivideCeil(width, numSteps);
        var invoker = new ColumnIntervalInvoker<TOperation>(rectangle, in operation, colsPerStep);

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

    public static void IterateColumnIntervals<TOperation, TBuffer>(Rectangle rectangle, TOperation operation, int? degreesOfParallelism = null)
        where TOperation : IColumnIntervalOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        degreesOfParallelism ??= Environment.ProcessorCount;
        degreesOfParallelism = Math.Clamp(degreesOfParallelism.Value, 1, Environment.ProcessorCount);

        int top = rectangle.Top;
        int bottom = rectangle.Bottom;
        int left = rectangle.Left;
        int right = rectangle.Right;
        int width = rectangle.Width;
        int area = rectangle.Area;

        int maxSteps = DivideCeil(area, MinPixelsPerTask);
        int numSteps = Math.Min(degreesOfParallelism.Value, maxSteps);

        // Do not parallelize a single batch
        if (numSteps == 1)
        {
            var cols = new ColumnInterval(top, bottom, left, right);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(rectangle.Area);
            try
            {
                Unsafe.AsRef(in operation).Invoke(in cols, buffer.Memory.Span);
            }
            finally
            {
                Unsafe.AsRef(in operation).Complete();
            }
            return;
        }

        var colsPerStep = DivideCeil(width, numSteps);
        var invoker = new ColumnIntervalWithBufferInvoker<TOperation, TBuffer>(rectangle, in operation, colsPerStep);

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
