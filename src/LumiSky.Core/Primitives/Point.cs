using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Primitives;

public readonly struct Point : IEquatable<Point>
{
    public static readonly Point Empty = default;

    public bool IsEmpty => Equals(Empty);

    public int X { get; }

    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Point(Vector2 vector)
    {
        X = (int)vector.X;
        Y = (int)vector.Y;
    }

    public Point(Size size)
        : this(size.Width, size.Height)
    {
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Add(Point point, Size size)
    {
        return new Point(unchecked(point.X + size.Width), unchecked(point.Y + size.Height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Subtract(Point point, Size size)
    {
        return new Point(unchecked(point.X - size.Width), unchecked(point.Y - size.Height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Ceiling(PointF point)
    {
        return new Point(unchecked((int) MathF.Ceiling(point.X)), unchecked((int) MathF.Ceiling(point.Y)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Round(PointF point)
    {
        return new Point(unchecked((int) MathF.Round(point.X)), unchecked((int) MathF.Round(point.Y)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Truncate(PointF point)
    {
        return new Point(unchecked((int) point.X), unchecked((int) point.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point Offset(int dx, int dy) => new Point(X + dx, Y + dy);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point Offset(Point point) => Offset(point.X, point.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"(X={X}, Y={Y})";

    public override bool Equals(object? obj) => obj is Point other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Point other) => X == other.X && Y == other.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator PointF(Point point) => new PointF(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator System.Drawing.Point(Point point) => new System.Drawing.Point(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Point point) => new Vector2(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Point(Vector2 vector) => new Point(vector);

    public static Point operator +(Point point, Size size) => Add(point, size);

    public static Point operator -(Point point, Size size) => Subtract(point, size);

    public static bool operator ==(Point left, Point right) => left.Equals(right);

    public static bool operator !=(Point left, Point right) => !left.Equals(right);
}
