using OdinEye.Core.Primitives;
using System.Runtime.CompilerServices;

namespace OdinEye.Core.Memory;

public readonly struct Memory3D<T> : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _channels;
    private readonly Size _size;
    private readonly int _lengthPerChannel;
    private readonly int _length;
    private readonly Memory2D<T>[] _memoryOwners;

    internal Memory3D(int width, int height, List<Memory2D<T>> channels)
    {
        if ((uint)width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if ((uint)height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (channels.Count == 0) throw new ArgumentException("Channels is empty", nameof(channels));

        _width = width;
        _height = height;
        _channels = channels.Count;
        _size = new Size(width, height);
        _lengthPerChannel = width * height;
        _length = width * height * channels.Count;

        _memoryOwners = channels.ToArray();
    }

    public Memory3D(int width, int height, int channels = 1)
    {
        if ((uint)width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if ((uint)height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        if ((uint)channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));

        _width = width;
        _height = height;
        _channels = channels;
        _size = new Size(width, height);
        _lengthPerChannel = width * height;
        _length = width * height * channels;

        _memoryOwners = new Memory2D<T>[channels];
        for (int i = 0; i < channels; i++)
        {
            _memoryOwners[i] = new Memory2D<T>(width, height);
        }
    }

    public Memory3D(Size size, int channels = 1)
        : this(size.Width, size.Height, channels)
    {
    }

    public Memory3D(Memory2D<T> memory)
    {
        _width = memory.Width;
        _height = memory.Height;
        _channels = 1;
        _size = memory.Size;
        _lengthPerChannel = memory.Length;
        _length = memory.Length;

        // Memory3D takes ownership
        _memoryOwners = new Memory2D<T>[1];
        _memoryOwners[0] = memory;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_memoryOwners is not null)
        {
            for (int i = 0; i < _memoryOwners.Length; i++)
            {
                _memoryOwners[i].Dispose();
            }
        }
    }

    public Memory3D<T> Clone()
    {
        var clone = new Memory3D<T>(Width, Height, Channels);
        for (int c = 0; c < Channels; c++)
        {
            var src = GetChannel(c).GetSpan();
            var dst = clone.GetChannel(c).GetSpan();
            src.CopyTo(dst);
        }
        return clone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory2D<T> GetChannel(int channel = 0)
    {
#if DEBUG
        if ((uint)channel >= Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
        return _memoryOwners[channel];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int channel = 0)
    {
#if DEBUG
        if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
        return _memoryOwners[channel].GetSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetReadOnlySpan(int channel = 0) => GetSpan(channel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetRowSpan(int y, int channel = 0)
    {
#if DEBUG
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
        return _memoryOwners[channel].GetSpan().Slice(y * Width, Width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetRowReadOnlySpan(int y, int channel = 0) => GetRowSpan(y, channel);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetMemory(int channel = 0)
    {
#if DEBUG
        if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
        return _memoryOwners[channel].GetMemory();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetRowMemory(int y, int channel = 0)
    {
#if DEBUG
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
        return _memoryOwners[channel].GetMemory().Slice(y * Width, Width);
    }

    public readonly ref Memory2D<T> this[int channel]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if DEBUG
            if ((uint)channel >= (uint)Channels) throw new ArgumentOutOfRangeException(nameof(channel));
#endif
            return ref _memoryOwners[channel];
        }
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

    public int Channels
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _channels;
    }

    public Size Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _size;
    }

    public int LengthPerChannel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _lengthPerChannel;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }
}
