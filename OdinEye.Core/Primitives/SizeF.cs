using System.Runtime.CompilerServices;

namespace OdinEye.Core.Primitives;

public readonly struct SizeF : IEquatable<SizeF>
{
    public static readonly SizeF Empty = default;

    public bool IsEmpty => Equals(Empty);

    public float Width { get; }

    public float Height { get; }

    public SizeF(float value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

        Width = value;
        Height = value;
    }

    public SizeF(float width, float height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
    }

    public void Deconstruct(out float width, out float height)
    {
        width = Width;
        height = Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizeF Add(SizeF left, SizeF right)
    {
        return new SizeF(left.Width + right.Width, left.Height + right.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SizeF Subtract(SizeF left, SizeF right)
    {
        return new SizeF(left.Width - right.Width, left.Height - right.Height);
    }

    public override int GetHashCode() => HashCode.Combine(Width, Height);

    public override string ToString() => $"SizeF {{ Width={Width}, Height={Height} }}";

    public override bool Equals(object? obj) => obj is SizeF other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SizeF other) => Width == other.Width && Height == other.Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Size(SizeF size)
    {
        return new Size(unchecked((int) size.Width), unchecked((int) size.Height));
    }

    public static SizeF operator +(SizeF left, SizeF right) => Add(left, right);

    public static SizeF operator -(SizeF left, SizeF right) => Subtract(left, right);

    public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);

    public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);
}
