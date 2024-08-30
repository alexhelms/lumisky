using OdinEye.Core.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OdinEye.Core.Memory;

public readonly ref struct ReadOnlySpan2D<T>
{
    private readonly ref readonly T _reference;
    private readonly int _width;
    private readonly int _height;

    internal ReadOnlySpan2D(ref T value, int width, int height)
    {
        _reference = ref value;
        _width = width;
        _height = height;
    }

    public unsafe ReadOnlySpan2D(void* pointer, int width, int height)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) throw new ArgumentException("Can't us a void* constructor when T is a managed type.");
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);

        _reference = ref Unsafe.AsRef<T>(pointer);
        _width = width;
        _height = height;
    }

    public ReadOnlySpan2D(ReadOnlySpan<T> span, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);
        if (span.Length != width * height) throw new ArgumentOutOfRangeException("Length of span must equal width * height.");

        if (width == 0 || height == 0)
        {
            this = default;
            return;
        }

        _reference = ref span.GetReference();
        _width = width;
        _height = height;
    }

    public static ReadOnlySpan2D<T> Empty => default;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _width == 0 || _height == 0;
    }

    public int Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _width;
    }

    public int Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _height;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _width * _height;
    }

    public Size Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Size(_width, _height);
    }

    public ref readonly T this[int row, int column]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)row >= (uint)_height ||
                (uint)column >= (uint)_width)
            {
                throw new IndexOutOfRangeException();
            }

            return ref GetReferenceAt(row, column);
        }
    }

    public void CopyTo(Span<T> destination)
    {
        if (IsEmpty) return;

        if (TryGetSpan(out ReadOnlySpan<T> span))
        {
            span.CopyTo(destination);
        }
        else
        {
            if (Length > destination.Length)
                throw new ArgumentException("The target span is too short to to copy all the items to.");

            nint width = (nint)(uint)_width;

            ref T destinationRef = ref MemoryMarshal.GetReference(destination);

            for (int i = 0; i < _height; i++)
            {
                ref T sourceStart = ref GetReferenceAt(i, 0);
                ref T sourceEnd = ref Unsafe.Add(ref sourceStart, width);

                while (Unsafe.IsAddressLessThan(ref sourceStart, ref sourceEnd))
                {
                    destinationRef = sourceStart;

                    sourceStart = ref Unsafe.Add(ref sourceEnd, 1);
                    destinationRef = ref Unsafe.Add(ref destinationRef, 1);
                }
            }
        }
    }

    public void CopyTo(Span2D<T> destination)
    {
        if (destination.Height != Height ||
            destination.Width != Width)
            throw new ArgumentException("The target span is not the same shape");

        if (IsEmpty) return;

        if (destination.TryGetSpan(out Span<T> span))
        {
            CopyTo(span);
        }
        else
        {
            nint width = (nint)(uint)_width;

            for (int i = 0; i < _height; i++)
            {
                ref T sourceStart = ref GetReferenceAt(i, 0);
                ref T sourceEnd = ref Unsafe.Add(ref sourceStart, width);
                ref T destinationRef = ref destination.GetReferenceAt(i, 0);

                while (Unsafe.IsAddressLessThan(ref sourceStart, ref sourceEnd))
                {
                    destinationRef = sourceStart;

                    sourceStart = ref Unsafe.Add(ref sourceStart, 1);
                    destinationRef = ref Unsafe.Add(ref destinationRef, 1);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref readonly T GetPinnableReference()
    {
        ref readonly T r0 = ref Unsafe.AsRef<T>(null);

        if (Length != 0)
        {
            r0 = ref _reference;
        }

        return ref r0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReference()
    {
        return ref Unsafe.AsRef(in _reference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReferenceAt(int i, int j)
    {
        ref T r0 = ref Unsafe.AsRef(in _reference);
        nint index = ((nint)(uint)i * (nint)(uint)_width) + (nint)(uint)j;
        return ref Unsafe.Add(ref r0, index);
    }

    public unsafe ReadOnlySpan2D<T> Slice(int row, int column, int height, int width)
    {
        if ((uint)row >= _height) throw new ArgumentOutOfRangeException(nameof(row));
        if ((uint)column >= _width) throw new ArgumentOutOfRangeException(nameof(column));
        if ((uint)height >= _height - row) throw new ArgumentOutOfRangeException(nameof(height));
        if ((uint)width >= _width - column) throw new ArgumentOutOfRangeException(nameof(width));

        nint shift = ((nint)(uint)_width * row) + (nint)(uint)column;
        ref T r0 = ref Unsafe.Add(ref Unsafe.AsRef(in _reference), shift);
        return new(ref r0, height, width);
    }

    public ReadOnlySpan<T> GetRowSpan(int row)
    {
        if ((uint)row >= _height) throw new ArgumentOutOfRangeException(nameof(row));
        ref T r0 = ref GetReferenceAt(row, 0);
        return MemoryMarshal.CreateReadOnlySpan(ref r0, _width);
    }

    public bool TryGetSpan(out ReadOnlySpan<T> span)
    {
        span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _reference), Length);
        return true;
    }

    public override string ToString()
    {
        return $"ReadOnlySpan2D<{typeof(T)}>[{Width}, {Height}]";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        throw new NotSupportedException("Equals() on Span will always throw an exception. Use == instead.");
    }

    public override int GetHashCode()
    {
        throw new NotSupportedException("GetHashCode() on Span will always throw an exception.");
    }

    public static bool operator ==(ReadOnlySpan2D<T> left, ReadOnlySpan2D<T> right)
    {
        return Unsafe.AreSame(ref Unsafe.AsRef(in left._reference), ref Unsafe.AsRef(in right._reference)) &&
            left._height == right._height &&
            left._width == right._width;
    }

    public static bool operator !=(ReadOnlySpan2D<T> left, ReadOnlySpan2D<T> right)
    {
        return !(left == right);
    }

    public static implicit operator ReadOnlySpan2D<T>(Span2D<T> span)
    {
        return new(ref span.GetReference(), span.Width, span.Height);
    }
}
