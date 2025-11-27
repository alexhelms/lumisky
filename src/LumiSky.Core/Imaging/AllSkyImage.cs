using LumiSky.Core.Mathematics;
using LumiSky.Core.Memory;
using LumiSky.Core.Primitives;
using LumiSky.Core.Utilities;
using System.Numerics;

namespace LumiSky.Core.Imaging;

public partial class AllSkyImage : IDisposable
{
    public const int HistogramLength = 8192;

    public Memory3D<float> Data { get; }

    private PropertyCache PropCache { get; }
    private ReaderWriterLockSlim Lock { get; } = new(LockRecursionPolicy.SupportsRecursion);

    public ImageMetadata Metadata { get; } = new();
    public int Width => Data.Width;
    public int Height => Data.Height;
    public Size Size => Data.Size;
    public int Channels => Data.Channels;
    public int PixelsPerChannel => Width * Height;
    public int Count => Width * Height * Channels;
    public Rectangle Bounds => new Rectangle(0, 0, Width, Height);

    public AllSkyImage(int width, int height, int channels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(channels, 1);

        Data = new Memory3D<float>(width, height, channels);
        PropCache = new(channels);
    }

    public AllSkyImage(int width, int height)
        : this(width, height, 1)
    {
    }

    public AllSkyImage(Size size, int channels)
        : this(size.Width, size.Height, channels)
    {
    }

    internal AllSkyImage(Memory2D<byte> data)
        : this(data.Size, 1)
    {
        ReadOnlySpan<byte> src = data.GetSpan();
        Span<float> dst = Data.GetSpan();
        ImagingUtil.UInt8ToFloat(src, dst);
    }

    internal AllSkyImage(Memory3D<byte> data)
       : this(data.Size, data.Channels)
    {
        for (int c = 0; c < Channels; c++)
        {
            ReadOnlySpan<byte> src = data.GetSpan(c);
            Span<float> dst = Data.GetSpan(c);
            ImagingUtil.UInt8ToFloat(src, dst);
        }
    }

    internal AllSkyImage(Memory2D<ushort> data)
        : this(data.Size, 1)
    {
        ReadOnlySpan<ushort> src = data.GetSpan();
        Span<float> dst = Data.GetSpan();
        ImagingUtil.UInt16ToFloat(src, dst);
    }

    internal AllSkyImage(Memory3D<ushort> data)
        : this(data.Size, data.Channels)
    {
        for (int c = 0; c < Channels; c++)
        {
            ReadOnlySpan<ushort> src = data.GetSpan(c);
            Span<float> dst = Data.GetSpan(c);
            ImagingUtil.UInt16ToFloat(src, dst);
        }
    }

    internal AllSkyImage(Memory2D<float> data)
    {
        Data = new Memory3D<float>(data);
        PropCache = new(Channels);
    }

    internal AllSkyImage(Memory3D<float> data)
    {
        Data = data;
        PropCache = new(Channels);
    }

    internal AllSkyImage(Emgu.CV.Mat mat)
    {
        Data = new Memory3D<float>(mat.Cols, mat.Rows, mat.NumberOfChannels);
        PropCache = new(Channels);
        this.FromMat(mat);
    }

    private AllSkyImage(AllSkyImage other)
    {
        Data = other.Data.Clone();
        Metadata = other.Metadata.Clone();
        PropCache = new(Channels);
    }

    #region IDisposable

    private bool _disposed;

    ~AllSkyImage()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        OnDispose();

        _disposed = true;
    }

    protected virtual void OnDispose()
    {
        Lock.Dispose();
        Data.Dispose();
    }

    #endregion

    private void AssertChannel(int channel)
    {
        if (channel < 0 || channel >= Channels) throw new ArgumentOutOfRangeException(nameof(channel));
    }

    internal static float ScaleValue<T>(T value)
        where T : INumber<T>, IMinMaxValue<T>
    {
        if (ReflectionUtil.IsAssignableToGenericType(typeof(T), typeof(IFloatingPointIeee754<>)))
            return float.CreateSaturating(T.Clamp(value, T.Zero, T.One));
        else
            return float.CreateSaturating(value) / float.CreateSaturating(T.MaxValue);
    }

    public AllSkyImage Clone()
    {
        try
        {
            Lock.EnterReadLock();
            return new AllSkyImage(this);
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    public Memory3D<float> CopyData() => Data.Clone();

    public void Fill<T>(T value, int channel = 0)
        where T : INumber<T>, IMinMaxValue<T>
    {
        AssertChannel(channel);
        float scaledValue = ScaleValue(value);

        try
        {
            Lock.EnterWriteLock();
            Data[channel].GetSpan().Fill(scaledValue);
            PropCache.Clear();
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public void FillRandom(int channel = 0)
    {
        AssertChannel(channel);

        try
        {
            Lock.EnterWriteLock();
            var span = Data[channel].GetSpan();
            for (int i = 0; i < span.Length; i++)
                span[i] = Random.Shared.NextSingle();
            PropCache.Clear();
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        for (int c = 0; c < Channels; c++)
        {
            Clear(c);
        }
    }

    public void Clear(int channel)
    {
        AssertChannel(channel);

        try
        {
            Lock.EnterWriteLock();
            Data[channel].GetSpan().Clear();
            PropCache.Clear();
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public int[] Histogram(double? low = null, double? high = null, int channel = 0)
    {
        AssertChannel(channel);

        float lo = (float) low.GetValueOrDefault();
        float hi = (float) high.GetValueOrDefault();
        lo = Math.Clamp(lo, 0, 1);
        hi = Math.Clamp(hi, 0, 1);
        if (lo > hi) Util.Swap(ref low, ref high);

        var op = new HistogramOperation(this, channel, lo, hi);
        op.Run();

        return op.Histogram;
    }

    public double Variance(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("variance", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.Variance;
    }

    public double StdDev(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("stddev", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.StdDev;
    }

    public double Mean(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("mean", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.Mean;
    }

    public double Median(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("median", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.Median;
    }

    public double SubsampledMedian(int channel)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("median", channel, out var value))
            return (double)value!;

        double median = DoSubsampledMedian(channel);
        PropCache.Put("median", channel, median);
        return median;
    }

    public double MAD(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("mad", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.MAD;
    }

    public double Max(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("max", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.Maximum;
    }

    public double Min(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("min", channel, out var value))
            return (double)value!;

        var op = new StatisticsOperation(this, channel);
        op.Run();
        return op.Results.Minimum;
    }

    public (double, double) MinMax(int channel = 0)
    {
        AssertChannel(channel);

        if (PropCache.TryGetValue("min", channel, out var min) &&
            PropCache.TryGetValue("max", channel, out var max))
        {
            return ((double)min!, (double)max!);
        }

        var op = new MinMaxOperation(this, channel);
        op.Run();
        return (op.Min, op.Max);
    }

    public void Rescale(double lower = 0, double upper = 1.0, int channel = 0)
    {
        AssertChannel(channel);

        if (lower > upper) Util.Swap(ref lower, ref upper);

        float min = (float)Min();
        float max = (float)Max();

        if (Math.Abs(lower - min) < 1e-9 &&
            Math.Abs(upper - max) < 1e-9)
            return;

        float scale = min != max ? (float)(upper - lower) / (max - min) : 1.0f;
        new RescaleOperation(this, channel, min, max, scale, (float)lower, (float)upper).Run();
        PropCache.Clear();
    }

    public STF[] GetDefaultSTFs()
    {
        return Enumerable.Repeat(STF.Default, Channels).ToArray();
    }

    public STF[] GetSTFs(bool unlinked = false, bool boost = false)
    {
        STF[] stfs = new STF[Channels];

        if (unlinked)
        {
            for (int c = 0; c < Channels; c++)
            {
                stfs[c] = STF.Estimate(boost, Median(c), MAD(c));
            }
        }
        else
        {
            double[] medians = new double[Channels];
            double[] mads = new double[Channels];

            for (int c = 0; c < Channels; c++)
            {
                medians[c] = Median(c);
                mads[c] = MAD(c);
            }

            for (int c = 0; c < Channels; c++)
            {
                stfs[c] = STF.EstimateLinked(boost, medians, mads);
            }
        }

        return stfs;
    }

    public void Stretch(STF[] stfs)
    {
        if (stfs.Length != Channels)
            throw new ArgumentException($"{nameof(stfs)} length must match the number of channels");

        for (int c = 0; c < Channels; c++)
        {
            HistogramTransform.Apply(Data[c], stfs[c]);
        }
    }

    public void WhiteBalance(
        double redScale, double greenScale, double blueScale,
        double redBias, double greenBias, double blueBias)
    {
        if (Channels != 3) return;

        var minScale = LumiSkyMath.Min3(redScale, greenScale, blueScale);
        redScale /= minScale;
        greenScale /= minScale;
        blueScale /= minScale;

        Span<double> scales = [redScale, greenScale, blueScale];
        Span<double> biases = [redBias, greenBias, blueBias];
        double rgbAvgBias = (biases[0] + biases[1] + biases[2]) / 3.0;

        for (int c = 0; c < Channels; c++)
        {
            new ManualWhiteBalanceOperation(this, c, scales[c], biases[c], rgbAvgBias).Run();
        }
        
        PropCache.Clear();
    }

    public void WhiteBalance(double redScale, double greenScale, double blueScale)
    {
        if (Channels != 3) return;

        var medians = DoSubsampledMedian();
        WhiteBalance(redScale, greenScale, blueScale, medians[0], medians[1], medians[2]);
    }

    public void AutoSCurve(double contrast = 2.2)
    {
        var medians = DoSubsampledMedian();
        var minMedian = medians.Min();
        for (int c = 0; c < Channels; c++)
        {
            new AutoSCurveOperation(this, c, minMedian, contrast).Run();
        }

        PropCache.Clear();
    }

    public void BayerPixelCorrection(int? hotThresholdPercent, int? coldThresholdPercent)
    {
        if (Channels != 1)
            throw new InvalidOperationException($"{nameof(BayerPixelCorrection)} only works on 1 channel images");

        if (!hotThresholdPercent.HasValue && !coldThresholdPercent.HasValue)
            return;

        new BayerPixelCorrectionOperation(this, 0, hotThresholdPercent, coldThresholdPercent).Run();
        PropCache.Clear();
    }

    private class PropertyCache
    {
        private readonly Dictionary<(string, int), object> _cache = new();
        private readonly int _channels;

        public PropertyCache(int channels)
        {
            _channels = channels;
        }

        public void Clear() => _cache.Clear();

        public void Clear(int channel)
        {
            var keys = new List<(string, int)>();
            foreach (var key in _cache.Keys)
            {
                if (key.Item2 == channel)
                    keys.Add(key);
            }

            foreach (var key in keys)
                _cache.Remove(key);
        }

        public void Put(string key, int channel, object item)
        {
            if (channel < 0 || channel >= _channels) throw new ArgumentOutOfRangeException(nameof(channel));
            _cache[(key, channel)] = item;
        }

        public object GetValue(string key, int channel)
        {
            if (channel < 0 || channel >= _channels) throw new ArgumentOutOfRangeException(nameof(channel));
            return _cache[(key, channel)];
        }

        public bool TryGetValue(string key, int channel, out object? item)
        {
            if (channel < 0 || channel >= _channels) throw new ArgumentOutOfRangeException(nameof(channel));
            return _cache.TryGetValue((key, channel), out item);
        }

        public bool Contains(string key, int channel)
        {
            if (channel < 0 || channel >= _channels) throw new ArgumentOutOfRangeException(nameof(channel));
            return _cache.ContainsKey((key, channel));
        }
    }

    /// <summary>
    /// Get the median of each channel where every 16th element of the full data is sampled.
    /// </summary>
    private double[] DoSubsampledMedian()
    {
        var medians = new double[Channels];
        var tasks = new Task[Channels];
        for (int c = 0; c < Channels; c++)
        {
            int channel = c;
            tasks[channel] = Task.Run(() =>
            {
                medians[channel] = SubsampledMedian(channel);
            });
        }

        Task.WaitAll(tasks);

        return medians;
    }

    /// <summary>
    /// Get the median and mad of each channel where every 16th element of the full data is sampled.
    /// </summary>
    private (double[], double[]) DoSubsampledMedianAndMAD()
    {
        var medians = new double[Channels];
        var mads = new double[Channels];

        var tasks = new Task[Channels];
        for (int c = 0; c < Channels; c++)
        {
            int channel = c;
            tasks[channel] = Task.Run(() =>
            {
                (double median, double mad) = DoSubsampledMedianAndMAD(channel);
                medians[channel] = median;
                mads[channel] = mad;
            });
        }

        Task.WaitAll(tasks);

        return (medians, mads);
    }

    /// <summary>
    /// Get the median of each channel where every 16th element of the full data is sampled.
    /// </summary>
    private double DoSubsampledMedian(int channel = 0, int skip = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(skip, 1);
        AssertChannel(channel);

        using var mem = NativeMemoryAllocator<float>.Allocate(Width * Height / skip);
        var src = Data.GetReadOnlySpan(channel);
        var dst = mem.Memory.Span;
        for (int i = 0; i < src.Length; i += skip)
        {
            dst[i / skip] = src[i];
        }

        dst.Sort();
        return dst[dst.Length / 2];
    }

    /// <summary>
    /// Get the median and mad where every 16th element of the full data is sampled.
    /// </summary>
    private (double, double) DoSubsampledMedianAndMAD(int channel = 0, int skip = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(skip, 1);
        AssertChannel(channel);

        using var mem = NativeMemoryAllocator<float>.Allocate(Width * Height / skip);
        var src = Data.GetReadOnlySpan(channel);
        var dst = mem.Memory.Span;
        for (int i = 0; i < src.Length; i += skip)
        {
            dst[i / skip] = src[i];
        }

        dst.Sort();
        var median = dst[dst.Length / 2];

        foreach (ref float item in dst)
        {
            item = float.Abs(item - median);
        }

        dst.Sort();
        var mad = dst[dst.Length / 2];

        return (median, mad);
    }
}
