using System.Runtime.CompilerServices;

namespace OdinEye.Core.Primitives;

public readonly struct Size : IEquatable<Size>
{
    public static readonly Size Empty = default;

    public bool IsEmpty => Equals(Empty);

    public int Width { get; }

    public int Height { get; }

    public Size(int value)
    {
        Width = value;
        Height = value;
    }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Deconstruct(out int width, out int height)
    {
        width = Width;
        height = Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size Add(Size left, Size right)
    {
        return new Size(unchecked(left.Width + right.Width), unchecked(left.Height + right.Height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size Subtract(Size left, Size right)
    {
        return new Size(unchecked(left.Width - right.Width), unchecked(left.Height - right.Height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size Ceiling(SizeF size)
    {
        return new Size(unchecked((int) MathF.Ceiling(size.Width)), unchecked((int) MathF.Ceiling(size.Height)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size Round(SizeF size)
    {
        return new Size(unchecked((int) MathF.Round(size.Width)), unchecked((int) MathF.Round(size.Height)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size Truncate(SizeF size)
    {
        return new Size(unchecked((int) size.Width), unchecked((int) size.Height));
    }

    public override int GetHashCode() => HashCode.Combine(Width, Height);

    public override string ToString() => $"Size {{ Width={Width}, Height={Height} }}";

    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SizeF(Size size) => new SizeF(size.Width, size.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator System.Drawing.Size(Size size) => new System.Drawing.Size(size.Width, size.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Size(System.Drawing.Size size) => new Size(size.Width, size.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size operator +(Size left, Size right) => Add(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size operator -(Size left, Size right) => Subtract(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Size left, Size right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
}
