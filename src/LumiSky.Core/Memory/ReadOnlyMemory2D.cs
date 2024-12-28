using LumiSky.Core.Primitives;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Memory;

public readonly struct ReadOnlyMemory2D<T> : IEquatable<ReadOnlyMemory2D<T>>
{
    private readonly Memory2D<T> _memory;

    public ReadOnlyMemory2D(Memory2D<T> memory)
    {
        _memory = memory;
    }

    #region Equality

    public override bool Equals(object? obj)
    {
        return obj is ReadOnlyMemory2D<T> d && Equals(d);
    }

    public bool Equals(ReadOnlyMemory2D<T> other)
    {
        return EqualityComparer<Memory2D<T>>.Default.Equals(_memory, other._memory);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_memory);
    }

    public static bool operator ==(ReadOnlyMemory2D<T> left, ReadOnlyMemory2D<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReadOnlyMemory2D<T> left, ReadOnlyMemory2D<T> right)
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
    public ReadOnlySpan<T> GetSpan()
    {
        return _memory.GetSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetRowSpan(int y)
    {
        return _memory.GetRowSpan(y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<T> GetMemory()
    {
        return _memory.GetMemory();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<T> GetRowMemory(int y)
    {
        return _memory.GetRowMemory(y);
    }

    public ref readonly T this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _memory[x, y];
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Length == 0;
    }

    public int Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memory.Width;
    }

    public int Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memory.Height;
    }

    public Size Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memory.Size;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memory.Length;
    }
}
