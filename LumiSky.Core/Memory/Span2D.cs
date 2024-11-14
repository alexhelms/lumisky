using LumiSky.Core.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Memory;

public readonly ref struct Span2D<T>
{
    private readonly ref T _reference;
    private readonly int _width;
    private readonly int _height;

    internal Span2D(ref T value, int width, int height)
    {
        _reference = ref value;
        _width = width;
        _height = height;
    }

    public unsafe Span2D(void* pointer, int width, int height)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) throw new ArgumentException("Can't us a void* constructor when T is a managed type.");
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);

        _reference = ref Unsafe.AsRef<T>(pointer);
        _width = width;
        _height = height;
    }

    public Span2D(Span<T> span, int width, int height)
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

    public static Span2D<T> Empty => default;

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

    public ref T this[int row, int column]
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

    public void Clear()
    {
        if (IsEmpty) return;

        if (TryGetSpan(out Span<T> span))
        {
            span.Clear();
        }
        else
        {
            nint width = (nint)(uint)_width;

            for (int i = 0; i < _height; i++)
            {
                ref T rStart = ref GetReferenceAt(i, 0);
                ref T rEnd = ref Unsafe.Add(ref rStart, width);

                while (Unsafe.IsAddressLessThan(ref rStart, ref rEnd))
                {
                    rStart = default!;
                    rStart = ref Unsafe.Add(ref rStart, 1);
                }
            }
        }
    }

    public void CopyTo(Span<T> destination)
    {
        if (IsEmpty) return;

        if (TryGetSpan(out Span<T> span))
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

    public void Fill(T value)
    {
        if (IsEmpty) return;

        if (TryGetSpan(out Span<T> span))
        {
            span.Fill(value);
        }
        else
        {
            nint width = (nint)(uint)_width;

            for (int i = 0; i < _height; i++)
            {
                ref T rStart = ref GetReferenceAt(i, 0);
                ref T rEnd = ref Unsafe.Add(ref rStart, width);

                while (Unsafe.IsAddressLessThan(ref rStart, ref rEnd))
                {
                    rStart = value;

                    rStart = ref Unsafe.Add(ref rStart, 1);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T GetPinnableReference()
    {
        ref T r0 = ref Unsafe.AsRef<T>(null);
        
        if (Length != 0)
        {
            r0 = ref _reference;
        }

        return ref r0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReference()
    {
        return ref _reference;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReferenceAt(int i, int j)
    {
        nint index = ((nint)(uint)i * (nint)(uint)_width) + (nint)(uint)j;
        return ref Unsafe.Add(ref _reference, index);
    }

    public unsafe Span2D<T> Slice(int row, int column, int height, int width)
    {
        if ((uint)row >= _height) throw new ArgumentOutOfRangeException(nameof(row));
        if ((uint)column >= _width) throw new ArgumentOutOfRangeException(nameof(column));
        if ((uint)height >= _height - row) throw new ArgumentOutOfRangeException(nameof(height));
        if ((uint)width >= _width - column) throw new ArgumentOutOfRangeException(nameof(width));

        nint shift = ((nint)(uint)_width * row) + (nint)(uint)column;
        ref T r0 = ref Unsafe.Add(ref _reference, shift);
        return new(ref r0, height, width);
    }

    public Span<T> GetRowSpan(int row)
    {
        if ((uint)row >= _height) throw new ArgumentOutOfRangeException(nameof(row));
        ref T r0 = ref GetReferenceAt(row, 0);
        return MemoryMarshal.CreateSpan(ref r0, _width);
    }

    public bool TryGetSpan(out Span<T> span)
    {
        span = MemoryMarshal.CreateSpan(ref _reference, Length);
        return true;
    }

    public override string ToString()
    {
        return $"Span2D<{typeof(T)}>[{Width}, {Height}]";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        throw new NotSupportedException("Equals() on Span will always throw an exception. Use == instead.");
    }

    public override int GetHashCode()
    {
        throw new NotSupportedException("GetHashCode() on Span will always throw an exception.");
    }

    public static bool operator ==(Span2D<T> left, Span2D<T> right)
    {
        return Unsafe.AreSame(ref left._reference, ref right._reference) &&
            left._height == right._height &&
            left._width == right._width;
    }

    public static bool operator !=(Span2D<T> left, Span2D<T> right)
    {
        return !(left == right);
    }
}
