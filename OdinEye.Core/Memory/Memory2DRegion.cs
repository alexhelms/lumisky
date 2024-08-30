using OdinEye.Core.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OdinEye.Core.Memory;

public readonly struct Memory2DRegion<T>
{
    private readonly Memory2D<T> _data;
    private readonly Rectangle _region;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2DRegion(Memory2D<T> data, Rectangle region)
    {
        var dataRectangle = new Rectangle(0, 0, data.Size);
        if (!dataRectangle.Contains(region)) throw new ArgumentException("Data does not contain region", nameof(region));

        _data = data;
        _region = region;
    }

    public Memory2D<T> ToMemory2D()
    {
        var data = new Memory2D<T>(Size);
        for (int y = 0; y < Height; y++)
        {
            var src = GetRowSpan(y);
            var dst = data.GetRowSpan(y);
            src.CopyTo(dst);
        }
        return data;
    }

    public Rectangle Region => _region;

    public Size Size => Region.Size;

    public int X
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Region.X;
        }
    }

    public int Y
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Region.Y;
        }
    }

    public int Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Region.Width;
        }
    }

    public int Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Region.Height;
        }
    }

    public int Stride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _data.Width;
        }
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Width * Height;
        }
    }

    public ref T this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref _data.GetSpan()[(Y + y) * Stride + (X + x)];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetRowSpan(int y) => _data.GetRowSpan(Y + y).Slice(X, Width);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2DRegion<T> GetSubRegion(int x, int y, int width, int height)
        => GetSubRegion(new(x, y, width, height));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2DRegion<T> GetSubRegion(Rectangle rectangle)
    {
        Debug.Assert(rectangle.Width <= Width, "Subregion width must be less than or equal to than the parent");
        Debug.Assert(rectangle.Height <= Height, "Subregion height must be less than or equal to than the parent");

        var rect = new Rectangle(X + rectangle.X, Y + rectangle.Y, rectangle.Width, rectangle.Height);
        return new Memory2DRegion<T>(_data, rect);
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
            GetRowSpan(y).Clear();
    }
}
