using OdinEye.Core.Memory;
using OdinEye.Core.Primitives;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace OdinEye.Core.Imaging;

public static partial class ParallelRowIterator
{
    private readonly struct RowInvoker<TOperation>
        where TOperation : IRowOperation
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _rowsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RowInvoker(Rectangle rectangle, in TOperation operation, int rowsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _rowsPerStep = rowsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int yMin = _rectangle.Top + (i * _rowsPerStep);
            if (yMin >= _rectangle.Bottom)
                return;
            int yMax = Math.Min(yMin + _rowsPerStep, _rectangle.Bottom);

            for (int y = yMin; y < yMax; y++)
            {
                Unsafe.AsRef(in _operation).Invoke(y, _rectangle.Left, _rectangle.Width);
            }
        }
    }

    private readonly struct RowInvokerWithBuffer<TOperation, TBuffer>
        where TOperation : IRowOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _rowsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RowInvokerWithBuffer(Rectangle rectangle, in TOperation operation, int rowsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _rowsPerStep = rowsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int yMin = _rectangle.Top + (i * _rowsPerStep);
            if (yMin >= _rectangle.Bottom)
                return;
            int yMax = Math.Min(yMin + _rowsPerStep, _rectangle.Bottom);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(_rectangle.Width);

            for (int y = yMin; y < yMax; y++)
            {
                Unsafe.AsRef(in _operation).Invoke(y, _rectangle.Left, _rectangle.Width, buffer.Memory.Span);
            }
        }
    }

    private readonly struct RowIntervalInvoker<TOperation>
        where TOperation : IRowIntervalOperation
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _rowsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RowIntervalInvoker(Rectangle rectangle, in TOperation operation, int rowsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _rowsPerStep = rowsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int yMin = _rectangle.Top + (i * _rowsPerStep);
            if (yMin >= _rectangle.Bottom)
                return;
            int yMax = Math.Min(yMin + _rowsPerStep, _rectangle.Bottom);
            var rows = new RowInterval(yMin, yMax, _rectangle.Left, _rectangle.Right);
            Unsafe.AsRef(in _operation).Invoke(in rows);
        }
    }

    private readonly struct RowIntervalWithBufferInvoker<TOperation, TBuffer>
        where TOperation : IRowIntervalOperation<TBuffer>
        where TBuffer : struct, INumber<TBuffer>, IMinMaxValue<TBuffer>
    {
        private readonly Rectangle _rectangle;
        private readonly TOperation _operation;
        private readonly int _rowsPerStep;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RowIntervalWithBufferInvoker(Rectangle rectangle, in TOperation operation, int rowsPerStep)
        {
            _rectangle = rectangle;
            _operation = operation;
            _rowsPerStep = rowsPerStep;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(int i)
        {
            int yMin = _rectangle.Top + (i * _rowsPerStep);
            if (yMin >= _rectangle.Bottom)
                return;
            int yMax = Math.Min(yMin + _rowsPerStep, _rectangle.Bottom);
            var rows = new RowInterval(yMin, yMax, _rectangle.Left, _rectangle.Right);
            using var buffer = NativeMemoryAllocator<TBuffer>.Allocate(_rectangle.Area);
            Unsafe.AsRef(in _operation).Invoke(in rows, buffer.Memory.Span);
        }
    }
}
    
