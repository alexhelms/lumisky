using LumiSky.Core.Memory;
using LumiSky.Core.Primitives;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Imaging;

public static partial class ParallelColumnIterator
{
    private readonly struct ColumnInvoker<TOperation>
        where TOperation : IColumnOperation
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _colsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColumnInvoker(Rectangle rectangle, in TOperation operation, int colsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _colsPerStep = colsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int xMin = _rectangle.Left + (i * _colsPerStep);
            if (xMin >= _rectangle.Right)
                return;
            int xMax = Math.Min(xMin + _colsPerStep, _rectangle.Right);

            for (int x = xMin; x < xMax; x++)
            {
                Unsafe.AsRef(in _operation).Invoke(x, _rectangle.Top, _rectangle.Bottom);
            }
        }
    }

    private readonly struct ColumnInvokerWithBuffer<TOperation, TBuffer>
        where TOperation : IColumnOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _colsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColumnInvokerWithBuffer(Rectangle rectangle, in TOperation operation, int colsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _colsPerStep = colsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int xMin = _rectangle.Left + (i * _colsPerStep);
            if (xMin >= _rectangle.Right)
                return;
            int xMax = Math.Min(xMin + _colsPerStep, _rectangle.Right);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(_rectangle.Height);

            for (int x = xMin; x < xMax; x++)
            {
                Unsafe.AsRef(in _operation).Invoke(x, _rectangle.Top, _rectangle.Bottom, buffer.Memory.Span);
            }
        }
    }

    private readonly struct ColumnIntervalInvoker<TOperation>
        where TOperation : IColumnIntervalOperation
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _colsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColumnIntervalInvoker(Rectangle rectangle, in TOperation operation, int colsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _colsPerStep = colsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int xMin = _rectangle.Left + (i * _colsPerStep);
            if (xMin >= _rectangle.Right)
                return;
            int xMax = Math.Min(xMin + _colsPerStep, _rectangle.Right);
            var cols = new ColumnInterval(xMin, xMax, _rectangle.Left, _rectangle.Right);
            Unsafe.AsRef(in _operation).Invoke(in cols);
        }
    }

    private readonly struct ColumnIntervalWithBufferInvoker<TOperation, TBuffer>
        where TOperation : IColumnIntervalOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _colsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColumnIntervalWithBufferInvoker(Rectangle rectangle, in TOperation operation, int colsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _colsPerStep = colsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int xMin = _rectangle.Left + (i * _colsPerStep);
            if (xMin >= _rectangle.Right)
                return;
            int xMax = Math.Min(xMin + _colsPerStep, _rectangle.Right);
            var cols = new ColumnInterval(xMin, xMax, _rectangle.Left, _rectangle.Right);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(_rectangle.Area);
            Unsafe.AsRef(in _operation).Invoke(in cols, buffer.Memory.Span);
        }
    }
}
    
