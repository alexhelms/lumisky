using System.Runtime.CompilerServices;

namespace LumiSky.Core.Primitives;

public readonly struct Rectangle : IEquatable<Rectangle>
{
    public static readonly Rectangle Empty = default;

    public bool IsEmpty => Equals(Empty);

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Area { get; }

    public Point Location { get; }

    public Size Size { get; }

    public int Left => X;

    public int Top => Y;

    public int Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked(X + Width);
    }

    public int Bottom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked(Y + Height);
    }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Area = width * height;
        Location = new Point(x, y);
        Size = new Size(width, height);
    }

    public Rectangle(int x, int y, Size size)
        : this(x, y, size.Width, size.Height)
    {
    }

    public Rectangle(Point point, Size size)
        : this(point.X, point.Y, size.Width, size.Height)
    {
    }

    public void Deconstruct(out int x, out int y, out int width, out int height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RectangleF(Rectangle rectangle) => new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Center(Rectangle rectangle)
    {
        return new Point(rectangle.Left + (rectangle.Width / 2), rectangle.Top + (rectangle.Height / 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        int x1 = Math.Max(a.X, b.X);
        int x2 = Math.Min(a.Right, b.Right);
        int y1 = Math.Max(a.Y, b.Y);
        int y2 = Math.Min(a.Bottom, b.Bottom);

        if (x2 >= x1 && y2 >= y1)
        {
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        return Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Ceiling(RectangleF rectangle)
    {
        unchecked
        {
            return new Rectangle(
                (int)MathF.Ceiling(rectangle.X),
                (int)MathF.Ceiling(rectangle.Y),
                (int)MathF.Ceiling(rectangle.Width),
                (int)MathF.Ceiling(rectangle.Height));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Truncate(RectangleF rectangle)
    {
        unchecked
        {
            return new Rectangle(
                (int)rectangle.X,
                (int)rectangle.Y,
                (int)rectangle.Width,
                (int)rectangle.Height);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Round(RectangleF rectangle)
    {
        unchecked
        {
            return new Rectangle(
                (int)MathF.Round(rectangle.X),
                (int)MathF.Round(rectangle.Y),
                (int)MathF.Round(rectangle.Width),
                (int)MathF.Round(rectangle.Height));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle Union(Rectangle a, Rectangle b)
    {
        int x1 = Math.Min(a.X, b.X);
        int x2 = Math.Max(a.Right, b.Right);
        int y1 = Math.Min(a.Y, b.Y);
        int y2 = Math.Max(a.Bottom, b.Bottom);

        return new Rectangle(x1, y1, x2 - x1, y2 - y1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int x, int y) => X <= x && x < Right && Y <= y && y < Bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Point point) => Contains(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Rectangle rectangle) =>
            (X <= rectangle.X) && (rectangle.Right <= Right) &&
            (Y <= rectangle.Y) && (rectangle.Bottom <= Bottom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsWith(Rectangle rectangle) =>
            (rectangle.X < Right) && (X < rectangle.Right) &&
            (rectangle.Y < Bottom) && (Y < rectangle.Bottom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle Offset(int dx, int dy)
    {
        return new Rectangle(X + dx, Y + dy, Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle Offset(Point point) => Offset(point.X, point.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public override string ToString() => $"Rectangle {{ X={X}, Y={Y}, Width={Width}, Height={Height} }}";

    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Rectangle other) =>
        X == other.X &&
        Y == other.Y &&
        Width == other.Width &&
        Height == other.Height;
}
