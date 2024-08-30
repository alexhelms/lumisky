using System.Runtime.CompilerServices;

namespace OdinEye.Core.Primitives;

public readonly struct RectangleF
{
    public static readonly RectangleF Empty = default;

    public bool IsEmpty => Equals(Empty);

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }

    public PointF Location { get; }

    public SizeF Size { get; }

    public float Left => X;

    public float Top => Y;

    public float Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X + Width;
    }

    public float Bottom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Y + Height;
    }

    public RectangleF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Location = new PointF(x, y);
        Size = new SizeF(width, height);
    }

    public RectangleF(float x, float y, SizeF size)
        : this(x, y, size.Width, size.Height)
    {
    }

    public RectangleF(PointF point, SizeF size)
        : this(point.X, point.Y, size.Width, size.Height)
    {
    }

    public void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Rectangle(RectangleF rectangle) => Rectangle.Truncate(rectangle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RectangleF left, RectangleF right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RectangleF left, RectangleF right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF Center(RectangleF rectangle)
    {
        return new PointF(rectangle.Left + (rectangle.Width / 2), rectangle.Top + (rectangle.Height / 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF Intersect(RectangleF a, RectangleF b)
    {
        float x1 = MathF.Max(a.X, b.X);
        float x2 = MathF.Min(a.Right, b.Right);
        float y1 = MathF.Max(a.Y, b.Y);
        float y2 = MathF.Min(a.Bottom, b.Bottom);

        if (x2 >= x1 && y2 >= y1)
        {
            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        return Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF Union(RectangleF a, RectangleF b)
    {
        float x1 = MathF.Min(a.X, b.X);
        float x2 = MathF.Max(a.Right, b.Right);
        float y1 = MathF.Min(a.Y, b.Y);
        float y2 = MathF.Max(a.Bottom, b.Bottom);

        return new RectangleF(x1, y1, x2 - x1, y2 - y1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(float x, float y) => X <= x && x < Right && Y <= y && y < Bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(PointF point) => Contains(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(RectangleF rectangle) =>
            (X <= rectangle.X) && (rectangle.Right <= Right) &&
            (Y <= rectangle.Y) && (rectangle.Bottom <= Bottom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsWith(RectangleF rectangle) =>
            (rectangle.X < Right) && (X < rectangle.Right) &&
            (rectangle.Y < Bottom) && (Y < rectangle.Bottom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectangleF Offset(float dx, float dy)
    {
        return new RectangleF(X + dx, Y + dy, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectangleF Offset(PointF point) => Offset(point.X, point.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public override string ToString() => $"RectangleF {{ X={X}, Y={Y}, Width={Width}, Height={Height} }}";

    public override bool Equals(object? obj) => obj is RectangleF other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RectangleF other) =>
        X == other.X &&
        Y == other.Y &&
        Width == other.Width &&
        Height == other.Height;
}
