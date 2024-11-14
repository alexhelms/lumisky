using LumiSky.Core.Primitives;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Memory;

public readonly struct Memory2D<T> : IEquatable<Memory2D<T>>, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly IMemoryOwner<T> _memoryOwner;

    internal Memory2D(IMemoryOwner<T> buffer, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);

        _width = width;
        _height = height;
        _memoryOwner = buffer;
    }

    public Memory2D(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);

        _width = width;
        _height = height;
        _memoryOwner = NativeMemoryAllocator<T>.Allocate(Length);
    }

    public Memory2D(Size size)
        : this(size.Width, size.Height)
    {
    }

    public void Dispose()
    {
        _memoryOwner?.Dispose();
    }

    #region Equality

    public override bool Equals(object? obj)
    {
        return obj is Memory2D<T> d &&
               EqualityComparer<IMemoryOwner<T>>.Default.Equals(_memoryOwner, d._memoryOwner);
    }

    public bool Equals(Memory2D<T> other)
    {
        return EqualityComparer<IMemoryOwner<T>>.Default.Equals(_memoryOwner, other._memoryOwner);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_memoryOwner);
    }

    public static bool operator ==(Memory2D<T> left, Memory2D<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Memory2D<T> left, Memory2D<T> right)
    {
        return !(left == right);
    }

    #endregion

    public Memory2D<T> Clone()
    {
        var clone = new Memory2D<T>(Width, Height);
        var src = GetSpan();
        var dst = clone.GetSpan();
        src.CopyTo(dst);
        return clone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2DRegion<T> GetRegion(Rectangle rectangle) => new Memory2DRegion<T>(this, rectangle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2DRegion<T> GetRegion(Point center, Size size) => new Memory2DRegion<T>(this,
        new(center.X - size.Width / 2, center.Y - size.Height / 2, size));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan()
    {
        return _memoryOwner.Memory.Span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span2D<T> GetSpan2D()
    {
        return new Span2D<T>(_memoryOwner.Memory.Span, _width, _height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetRowSpan(int y)
    {
#if DEBUG
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
#endif
        return _memoryOwner.Memory.Span.Slice(y * Width, Width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetMemory()
    {
        return _memoryOwner.Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetRowMemory(int y)
    {
#if DEBUG
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
#endif
        return _memoryOwner.Memory.Slice(y * Width, Width);
    }

    public ReadOnlyMemory2D<T> AsReadOnly()
    {
        return new ReadOnlyMemory2D<T>(this);
    }

    public static implicit operator ReadOnlyMemory2D<T>(Memory2D<T> memory) => new ReadOnlyMemory2D<T>(memory);

    public readonly ref T this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if ((uint)x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
            if ((uint)y >= Height) throw new ArgumentOutOfRangeException(nameof(y));
#endif
            return ref GetRowSpan(y)[x];
        }
    }
    
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
        get => new(_width, _height);
    }

    public Rectangle Bounds
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0, 0, _width, _height);
    }
}
