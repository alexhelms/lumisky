using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Primitives;

public readonly struct PointF : IEquatable<PointF>
{
    public static readonly PointF Empty = default;

    public bool IsEmpty => Equals(Empty);

    public float X { get; }

    public float Y { get; }

    public PointF(float x, float y)
    {
        X = x;
        Y = y;
    }

    public PointF(Point p)
    {
        X = p.X;
        Y = p.Y;
    }

    public PointF(Vector2 vector)
    {
        X = vector.X;
        Y = vector.Y;
    }

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF Add(PointF point, SizeF size)
    {
        return new PointF(point.X + size.Width, point.Y + size.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF Add(PointF point, PointF pointb)
    {
        return new PointF(point.X + pointb.X, point.Y + pointb.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF Subtract(PointF point, SizeF size)
    {
        return new PointF(point.X - size.Width, point.Y - size.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF Subtract(PointF point, PointF pointb)
    {
        return new PointF(point.X - pointb.X, point.Y - pointb.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PointF Offset(float dx, float dy)
    {
        return new PointF(X + dx, Y + dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Offset(PointF point) => Offset(point.X, point.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"(X={X}, Y={Y})";

    public override bool Equals(object? obj) => obj is PointF other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(PointF other) => X == other.X && Y == other.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator PointF(Point point) => new PointF(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(PointF point) => new Vector2(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator PointF(Vector2 vector) => new PointF(vector);

    public static PointF operator +(PointF point, PointF size) => Add(point, size);

    public static PointF operator -(PointF point, PointF size) => Subtract(point, size);

    public static bool operator ==(PointF left, PointF right) => left.Equals(right);

    public static bool operator !=(PointF left, PointF right) => !left.Equals(right);
}
