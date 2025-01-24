using LumiSky.Core.Mathematics;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Imaging;

public partial class AllSkyImage
{
    private abstract class BaseOperation
    {
        protected readonly AllSkyImage image;
        protected readonly int channel;

        protected bool AcquireReadLock { get; set; }
        protected bool AcquireWriteLock { get; set; }

        public BaseOperation(AllSkyImage image, int channel)
        {
            this.image = image;
            this.channel = channel;
        }

        public void Run()
        {
            if (AcquireReadLock && AcquireWriteLock)
                throw new InvalidOperationException("Cannot acquire both a read and write lock, choose one.");

            try
            {
                if (AcquireWriteLock) image.Lock.EnterWriteLock();
                else if (AcquireReadLock) image.Lock.EnterReadLock();

                OnRun();
            }
            catch (Exception e)
            {
                Log.Error(e, $"Exception in {GetType().Name} operation");
            }
            finally
            {
                if (AcquireWriteLock) image.Lock.ExitWriteLock();
                else if (AcquireReadLock) image.Lock.ExitReadLock();
            }
        }

        public abstract void OnRun();
    }

    private abstract class BaseRowIntervalOperation : BaseOperation, IRowIntervalOperation
    {
        public BaseRowIntervalOperation(AllSkyImage image, int channel)
            : base(image, channel)
        {
        }

        public override void OnRun()
        {
            ParallelRowIterator.IterateRowIntervals(image.Bounds, this);
        }

        public abstract void Invoke(in RowInterval rows);

        public virtual void Complete() { }
    }

    private abstract class BaseColumnIntervalOperation : BaseOperation, IColumnIntervalOperation
    {
        public BaseColumnIntervalOperation(AllSkyImage image, int channel)
            : base(image, channel)
        {
        }

        public override void OnRun()
        {
            ParallelColumnIterator.IterateColumnIntervals(image.Bounds, this);
        }

        public abstract void Invoke(in ColumnInterval rows);

        public virtual void Complete() { }
    }

    private record StatisticsResults
    {
        public float Variance { get; init; }
        public float StdDev { get; init; }
        public float Mean { get; init; }
        public float Median { get; init; }
        public float MAD { get; init; }
        public float Maximum { get; init; }
        public float Minimum { get; init; }
    }

    private class StatisticsOperation : BaseOperation
    {
        public StatisticsResults Results { get; private set; } = new();

        public StatisticsOperation(AllSkyImage image, int channel)
            : base(image, channel)
        {
            AcquireReadLock = true;
        }

        public override void OnRun()
        {
            var stats = ComputeStatistics();

            image.PropCache.Put("variance", channel, (double)stats.Variance);
            image.PropCache.Put("stddev", channel, (double)stats.StdDev);
            image.PropCache.Put("mean", channel, (double)stats.Mean);
            image.PropCache.Put("median", channel, (double)stats.Median);
            image.PropCache.Put("mad", channel, (double)stats.MAD);
            image.PropCache.Put("max", channel, (double)stats.Maximum);
            image.PropCache.Put("min", channel, (double)stats.Minimum);

            Results = stats;
        }

        private StatisticsResults ComputeStatistics()
        {
            double sum = 0;
            float variance = 0;
            float stdDev = 0;
            float mean = 0;
            float median = 0;
            float mad = 0;
            float maximum = 0;
            float minimum = 1;
            int count = image.PixelsPerChannel;

            if (image.PropCache.TryGetValue("median", channel, out var medianObj))
                median = Convert.ToSingle(medianObj);
            else
                median = DoMedian(image, channel);

            if (image.PropCache.TryGetValue("mad", channel, out var madObj))
                mad = Convert.ToSingle(madObj);
            else
                mad = DoMAD(image, channel, median) * 1.4826f;

            Span<float> data = image.Data.GetSpan(channel);

            for (int i = 0; i < count; i++)
                sum += data[i];

            mean = (float)(sum / count);

            float s = 0, ep = 0;
            for (int i = 0; i < count; i++)
            {
                s = data[i] - mean;
                ep += s;
                variance += s * s;

                if (data[i] < minimum)
                    minimum = data[i];
                if (data[i] > maximum)
                    maximum = data[i];
            }

            variance = (variance - ep * ep / count);
            if (count > 1)
                variance /= (count - 1);
            stdDev = float.Sqrt(variance);

            return new StatisticsResults
            {
                Variance = variance,
                StdDev = stdDev,
                Mean = mean,
                Median = median,
                MAD = mad,
                Maximum = maximum,
                Minimum = minimum,
            };
        }

        static float DoMedian(AllSkyImage self, int channel)
        {
            var minMaxOp = new MinMaxOperation(self, channel);
            minMaxOp.Run();

            if (minMaxOp.Min == 0 && minMaxOp.Max == 0) return 0;
            if (minMaxOp.Min == 1 && minMaxOp.Max == 1) return 1;

            var count = self.Width * self.Height;
            var low = minMaxOp.Min;
            var high = minMaxOp.Max;
            float eps = 1e-20f;

            if (high - low < eps)
                return low;

            float mh = 0, l0 = low, h0 = high;
            int[] H = new int[HistogramLength];

            // Iteratively find the median using a histogram
            for (int n = 0, n2 = count >> 1, step = 0, it = 0; ; ++it)
            {
                H.AsSpan().Clear();
                var op = new HistogramOperation(self, channel, H, low, high);
                op.Run();

                // Start at the beginning of the histogram
                // and add each bin until n is greater than
                // half of the number of total pixels.
                for (int i = 0; ; n += H[i++])
                {
                    if (i == H.Length || n + H[i] > n2)
                    {
                        // Rescale the low and high, a new histogram may be computed
                        float range = high - low;

                        high = ((range * (i + 1)) / HistogramLength) + low;
                        low = ((range * i) / HistogramLength) + low;

                        // Have we converged on the median?
                        if (high - low < eps)
                        {
                            if (count % 2 == 1)
                                return low;
                            if (step != 0)
                                return (low + mh) / 2;

                            mh = low;
                            low = l0;
                            high = h0;
                            n = 0;
                            --n2;
                            ++step;
                            it = 0;
                        }

                        break;
                    }
                }
            }
        }

        static float DoMAD(AllSkyImage self, int channel, float median)
        {
            var minMaxOp = new AbsDevMinMaxOperation(self, channel, median);
            minMaxOp.Run();

            if (minMaxOp.Min == 0 && minMaxOp.Max == 0) return 0;
            if (minMaxOp.Min == 1 && minMaxOp.Max == 1) return 0;

            var count = self.Width * self.Height;
            var low = minMaxOp.Min;
            var high = minMaxOp.Max;
            float eps = 1e-20f;

            float mh = 0, l0 = low, h0 = high;
            int[] H = new int[HistogramLength];

            // Iteratively find the MAD using a histogram
            for (int n = 0, n2 = count >> 1, step = 0, it = 0; ; ++it)
            {
                H.AsSpan().Clear();
                var op = new AbsDevHistogramOperation(self, channel, H, low, high, median);
                op.Run();

                // Start at the beginning of the histogram
                // and add each bin until n is greater than
                // half of the number of total pixels.
                for (int i = 0; ; n += H[i++])
                {
                    if (i == H.Length || n + H[i] > n2)
                    {
                        // Rescale the low and high, a new histogram may be computed
                        float range = high - low;
                        high = ((range * (i + 1)) / HistogramLength) + low;
                        low = ((range * i) / HistogramLength) + low;

                        // Have we converged on the median?
                        if (high - low < eps)
                        {
                            if (count % 2 == 1)
                                return low;
                            if (step != 0)
                                return (low + mh) / 2;

                            mh = low;
                            low = l0;
                            high = h0;
                            n = 0;
                            --n2;
                            ++step;
                            it = 0;
                        }

                        break;
                    }
                }
            }
        }
    }

    private class MinMaxOperation : BaseRowIntervalOperation
    {
        private readonly ConcurrentBag<float> minBag = new();
        private readonly ConcurrentBag<float> maxBag = new();

        private float min, max;

        /// <summary>
        /// Min value, normalized.
        /// </summary>
        public float Min => min;

        // Max value, normalized.
        public float Max => max;

        public MinMaxOperation(AllSkyImage image, int channel)
            : base(image, channel)
        {
            AcquireReadLock = true;
        }

        public override void Invoke(in RowInterval rows)
        {
            float min = 1;
            float max = 0;

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    float value = rowSpan[x];
                    if (value < min)
                        min = value;
                    if (value > max)
                        max = value;
                    if (min == 0 && max == 1)
                        goto exit;
                }
            }

        exit:
            minBag.Add(min);
            maxBag.Add(max);
        }

        public override void Complete()
        {
            min = minBag.Min();
            max = maxBag.Max();
        }
    }

    private class AbsDevMinMaxOperation : BaseRowIntervalOperation
    {
        private readonly float center;
        private readonly ConcurrentBag<float> minBag = new();
        private readonly ConcurrentBag<float> maxBag = new();

        private float min, max;

        /// <summary>
        /// Absolute deviation min value, normalized.
        /// </summary>
        public float Min => min;

        // Absolute deviation max value, normalized.
        public float Max => max;

        public AbsDevMinMaxOperation(AllSkyImage image, int channel, float center)
            : base(image, channel)
        {
            AcquireReadLock = true;
            this.center = center;
        }

        public override void Invoke(in RowInterval rows)
        {
            float min = 1;
            float max = 0;

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    float value = float.Abs(rowSpan[x] - center);
                    if (value < min)
                        min = value;
                    if (value > max)
                        max = value;
                    if (min == 0 && max == 1)
                        goto exit;
                }
            }

        exit:
            minBag.Add(min);
            maxBag.Add(max);
        }

        public override void Complete()
        {
            min = minBag.Min();
            max = maxBag.Max();
        }
    }

    private class HistogramOperation : BaseRowIntervalOperation
    {
        private readonly float low;
        private readonly float high;
        private readonly float range;

        public int[] Histogram { get; }

        public HistogramOperation(AllSkyImage image, int channel, float low, float high)
            : base(image, channel)
        {
            AcquireReadLock = true;
            Histogram = new int[HistogramLength];
            this.low = low;
            this.high = high;
            this.range = high - low;
        }

        public HistogramOperation(AllSkyImage image, int channel, int[] histogram, float low, float high)
            : base(image, channel)
        {
            if (histogram.Length != HistogramLength)
                throw new ArgumentException($"histogram length must be {HistogramLength}", nameof(histogram));

            AcquireReadLock = true;
            this.Histogram = histogram;
            this.low = low;
            this.high = high;
            this.range = high - low;
        }

        public override void Invoke(in RowInterval rows)
        {
            Span<int> histo = stackalloc int[HistogramLength];
            double bucketSize = range / HistogramLength;

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    float value = rowSpan[x];
                    if (value >= low && value <= high)
                    {
                        int i = (int)((value - low) / bucketSize);
                        if (i == HistogramLength)
                            i = HistogramLength - 1;
                        histo[i]++;
                    }
                }
            }

            ref var master = ref MemoryMarshal.GetReference(Histogram.AsSpan());
            for (int i = 0; i < histo.Length; i++)
                Interlocked.Add(ref Unsafe.Add(ref master, i), histo[i]);
        }
    }

    private class AbsDevHistogramOperation : BaseRowIntervalOperation
    {
        private readonly float low;
        private readonly float high;
        private readonly float center;
        private readonly float range;

        public int[] Histogram { get; }

        public AbsDevHistogramOperation(AllSkyImage image, int channel, float low, float high, float center)
            : base(image, channel)
        {
            AcquireReadLock = true;
            Histogram = new int[HistogramLength];
            this.low = low;
            this.high = high;
            this.center = center;
            this.range = high - low;
        }

        public AbsDevHistogramOperation(AllSkyImage image, int channel, int[] histogram, float low, float high, float center)
            : base(image, channel)
        {
            if (histogram.Length != HistogramLength)
                throw new ArgumentException($"histogram length must be {HistogramLength}", nameof(histogram));

            AcquireReadLock = true;
            this.Histogram = histogram;
            this.low = low;
            this.high = high;
            this.center = center;
            this.range = high - low;
        }

        public override void Invoke(in RowInterval rows)
        {
            Span<int> histo = stackalloc int[HistogramLength];
            double bucketSize = range / HistogramLength;

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    float value = float.Abs(rowSpan[x] - center);
                    if (value >= low && value <= high)
                    {
                        int i = (int)((value - low) / bucketSize);
                        if (i == HistogramLength)
                            i = HistogramLength - 1;
                        histo[i]++;
                    }
                }
            }

            ref var master = ref MemoryMarshal.GetReference(Histogram.AsSpan());
            for (int i = 0; i < histo.Length; i++)
                Interlocked.Add(ref Unsafe.Add(ref master, i), histo[i]);
        }
    }

    private class RescaleOperation : BaseRowIntervalOperation
    {
        private readonly float scale;
        private readonly float lower;
        private readonly float upper;

        private readonly float min;
        private readonly float max;

        public RescaleOperation(AllSkyImage image, int channel, float min, float max, float scale, float lower, float upper)
            : base(image, channel)
        {
            AcquireWriteLock = true;
            this.min = min;
            this.max = max;
            this.scale = scale;
            this.lower = lower;
            this.upper = upper;
        }

        public override void Invoke(in RowInterval rows)
        {
            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                if (min != max)
                {
                    if (lower != upper)
                    {
                        for (int x = rows.Left; x < rows.Right; x++)
                        {
                            float  value = rowSpan[x];
                            float  rescaled = scale * (value - min) + lower;
                            rowSpan[x] = rescaled;
                        }
                    }
                    else
                    {
                        for (int x = rows.Left; x < rows.Right; x++)
                            rowSpan[x] = lower;
                    }
                }
                else
                {
                    float clamped = float.Clamp(min, lower, upper);
                    rowSpan.Fill(clamped);
                }
            }
        }
    }

    private class StretchOperation : BaseRowIntervalOperation
    {
        private readonly STF stf;

        private float[] mtfLut;

        public StretchOperation(AllSkyImage image, int channel, STF stf)
            : base(image, channel)
        {
            AcquireWriteLock = true;
            this.stf = stf;
            this.mtfLut = CreateLut(stf);
        }

        private static float[] CreateLut(STF stf)
        {
            const int LutSize = ushort.MaxValue + 1;
            var lut = ArrayPool<float>.Shared.Rent(LutSize);
            for (int i = 0; i < LutSize; i++)
            {
                double value = (double) i / ushort.MaxValue;
                lut[i] = (float)STF.MTF(stf.M, value);
            }
            return lut;
        }

        public override void Complete()
        {
            ArrayPool<float>.Shared.Return(mtfLut);
        }

        public override void Invoke(in RowInterval rows)
        {
            float d = 1.0f;
            bool hasClipping = stf.C0 != 0 || stf.C1 != 1.0;
            bool hasDelta = false;
            if (hasClipping)
            {
                d = (float)(stf.C1 - stf.C0);
                hasDelta = 1 + d != 1;
            }

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    float value = rowSpan[x];
                    int index = (int)(value * ushort.MaxValue);

                    if (hasClipping)
                    {
                        if (hasDelta)
                        {
                            if (value <= stf.C0)
                            {
                                value = 0;
                            }
                            else if (value >= stf.C1)
                            {
                                value = 1.0f;
                            }
                            else
                            {
                                value = (float)((value - stf.C0) / d);
                            }
                        }
                        else
                        {
                            value = (float)stf.C0;
                        }
                    }

                    value = mtfLut[index];
                    rowSpan[x] = value;
                }
            }
        }
    }

    private class ManualWhiteBalanceOperation : BaseRowIntervalOperation
    {
        private readonly double scale;
        private readonly double bias;
        private readonly double rgbAvgBias;

        public ManualWhiteBalanceOperation(AllSkyImage image, int channel, double scale, double bias, double rgbAvgBias)
            : base(image, channel)
        {
            AcquireWriteLock = true;
            this.scale = scale;
            this.bias = bias;
            this.rgbAvgBias = rgbAvgBias;
        }

        public override void Invoke(in RowInterval rows)
        {
            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    rowSpan[x] = float.Clamp((float)((rowSpan[x] - bias) * scale + rgbAvgBias), 0.0f, 1.0f);
                }
            }
        }
    }

    private class AutoSCurveOperation : BaseRowIntervalOperation
    {
        private float[] lut;
        private double median;
        private double contrast;
        private double inflection;

        public AutoSCurveOperation(AllSkyImage image, int channel, double median, double contrast)
            : base(image, channel)
        {
            AcquireWriteLock = true;
            this.median = double.Clamp(median, 1e-6, 1);
            this.contrast = double.Clamp(contrast, 1e-6, double.MaxValue);
            this.inflection = -1 * double.Log(2) / double.Log(this.median);
            this.lut = CreateLut(this.median, this.contrast, this.inflection);
        }

        private static float[] CreateLut(double median, double contrast, double inflection)
        {
            const int LutSize = ushort.MaxValue + 1;
            var lut = ArrayPool<float>.Shared.Rent(LutSize);
            ushort denormalizedMedian = (ushort)(median * ushort.MaxValue);
            for (int i = 0; i < LutSize; i++)
            {
                double value = (double) i / ushort.MaxValue;
                if (i < denormalizedMedian)
                {
                    // PixInsight Pixel Math
                    // (0.5 * (($T^B)/0.5)^a)^(1/B)
                    lut[i] = (float)double.Pow(0.5 * double.Pow(2 * double.Pow(value, inflection), contrast), 1 / inflection);
                }
                else
                {
                    // PixInsight Pixel Math
                    // (1 - 0.5 * ((1 - ($T^B))/0.5)^a)^(1/B)
                    lut[i] = (float)double.Pow(1 - 0.5 * double.Pow(2 * (1 - double.Pow(value, inflection)), contrast), 1 / inflection);
                }
            }
            return lut;
        }

        public override void Complete()
        {
            ArrayPool<float>.Shared.Return(lut);
        }

        public override void Invoke(in RowInterval rows)
        {
            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                var rowSpan = image.Data.GetRowSpan(y, channel);

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    int index = (int)(rowSpan[x] * ushort.MaxValue);
                    rowSpan[x] = lut[index];
                }
            }
        }
    }

    private class BayerHotPixelCorrectionOperation : BaseRowIntervalOperation
    {
        private int thresholdPercent;

        public BayerHotPixelCorrectionOperation(AllSkyImage image, int channel, int thresholdPercent)
            : base(image, channel)
        {
            AcquireWriteLock = true;
            this.thresholdPercent = thresholdPercent;
        }

        public override void Invoke(in RowInterval rows)
        {
            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                // skip the border
                if (y < 2 || y >= image.Height - 2) continue;

                var prevRowSpan = image.Data.GetRowSpan(y - 2, channel);
                var thisRowSpan = image.Data.GetRowSpan(y, channel);
                var nextRowSpan = image.Data.GetRowSpan(y + 2, channel);

                int left = int.Max(2, rows.Left);
                int right = int.Min(image.Width - 2, rows.Right);
                for (int x = left; x < right; x++)
                {
                    // Get NSEW pixel of the same color
                    float n = prevRowSpan[x];
                    float s = nextRowSpan[x];
                    float e = thisRowSpan[x + 2];
                    float w = thisRowSpan[x - 2];
                    float c = thisRowSpan[x];

                    float maxValue = LumiSkyMath.Max4(n, s, e, w);
                    if (c > maxValue + (maxValue * (thresholdPercent / 100.0f)))
                    {
                        float average = (n + s + e + w) / 4.0f;
                        thisRowSpan[x] = (float)average;
                    }
                }
            }
        }
    }
}
