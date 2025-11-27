using System.Collections.Concurrent;

namespace LumiSky.Core.Imaging;

[Flags]
public enum STFFlags
{
    None = 0,
    Clipping = 1,
    Delta = 2,
}

/// <summary>
/// Screen transfer function for histogram transformations.
/// </summary>
public readonly struct STF : IEquatable<STF>
{
    public const double DefaultShadows = 0.0;
    public const double DefaultMidtones = 0.5;
    public const double DefaultHighlights = 1.0;

    // Default shawdow clipping point in normalized MAD units from the median.
    public const double DefaultLowClip = -2.8;

    // Target background median for the transformed image, normalized [0, 1].
    public const double DefaultTargetMedian = 0.25;

    // Boost factor for low clipping point.
    public const double LowClipBoostFactor = 0.75;

    // Boost factor for target background median.
    public const double TargetMedianBoostFactor = 2.0;

    /// <summary>
    /// Midtones point, normalized [0, 1].
    /// </summary>
    public double Midtones { get; }

    /// <summary>
    /// Shadow clip point, normalized [0, 1].
    /// </summary>
    public double Shadows { get; }

    /// <summary>
    /// Highlights clip point, normalized [0, 1].
    /// </summary>
    public double Highlights { get; }

    public STFFlags Flags { get; }

    public STF(double shadows, double midtones, double highlights)
    {
        Shadows = double.Clamp(shadows, 0, 1);
        Midtones = double.Clamp(midtones, 0, 1); ;
        Highlights = double.Clamp(highlights, 0, 1); ;

        ArgumentOutOfRangeException.ThrowIfGreaterThan(shadows, highlights);

        if (shadows != DefaultShadows || highlights != DefaultHighlights)
        {
            Flags |= STFFlags.Clipping;
        }

        if (double.Abs(highlights - shadows) > double.Epsilon)
        {
            Flags |= STFFlags.Delta;
        }
    }

    public STF(double midtones)
        : this(DefaultShadows, midtones, DefaultHighlights)
    {
    }

    public static readonly STF Default = new(DefaultShadows, DefaultMidtones, DefaultHighlights);

    public readonly override string ToString() => $"STF {{{Shadows:F6}, {Midtones:F6}, {Highlights:F6}}}";

    #region Equality

    public override bool Equals(object? obj)
    {
        return obj is STF stf && Equals(stf);
    }

    public bool Equals(STF other)
    {
        return Midtones == other.Midtones &&
               Shadows == other.Shadows &&
               Highlights == other.Highlights;
    }

    public override int GetHashCode() => HashCode.Combine(Shadows, Midtones, Highlights);

    public static bool operator ==(STF left, STF right) => left.Equals(right);

    public static bool operator !=(STF left, STF right) => !(left == right);

    #endregion

    /// <summary>
    /// Midtones Transfer Function.
    /// Returns the value of the MTF for given midtone balance and sample points.
    /// </summary>
    /// <remarks>
    /// Bulirsch-Stoer algorithm for a diagonal rational interpolation
    /// function with three fixed data points: (0,0) (m,1/2) (1,1).
    /// 
    /// This is the MTF function after direct substitution in the B-S
    /// equations:
    /// 
    ///    double r = 1 + 0.5/((x - m)/(x - 1)/2 - 1);
    ///    return r + r/(x/(x - 1) * (1 - r/(r - 0.5)) - 1);
    /// 
    /// We can simplify:
    /// 
    ///    double r = (m - 1)/(x + m - 2);
    /// 
    /// and then we can further simplify to:
    /// 
    ///    return (m - 1)*x/((2*m - 1)*x - m);
    /// 
    /// Finally, precalculating (m - 1) we can save a multiplication.
    /// </remarks>
    /// <param name="m">Midtone balance point, normalized [0, 1]</param>
    /// <param name="x">Sample point, normalized [0, 1]</param>
    /// <returns>The transformed value.</returns>
    public static double MTF(double m, double x)
    {
        if (x > 0)
        {
            if (x < 1)
            {
                double m1 = m - 1.0;
                return m1 * x / ((m + m1) * x - m);
            }
            return 1.0;
        }
        return 0.0;
    }

    /// <summary>
    /// Estimate the optimal Screen Transfer Function.
    /// </summary>
    /// <param name="boost">True to estimate a more aggressive STF.</param>
    /// <param name="median">Median, normalized [0, 1]</param>
    /// <param name="mad">MAD, normalized [0, 1]</param>
    /// <returns>The Optimal STF.</returns>
    public static STF Estimate(bool boost, double median, double mad)
    {
        double lowClip = DefaultLowClip;
        double targetBackground = DefaultTargetMedian;
        if (boost)
        {
            lowClip *= LowClipBoostFactor;
            targetBackground *= TargetMedianBoostFactor;
        }

        double c0, m, c1;
        if (median < 0.5)
        {
            c0 = double.Clamp(median + lowClip * mad, 0.0, 1.0);
            m = MTF(targetBackground, median - c0);
            c1 = 1.0;
        }
        else // Inverted
        {
            c1 = double.Clamp(median - lowClip * mad, 0.0, 1.0);
            m = MTF(c1 - median, targetBackground);
            c0 = 0;
        }

        return new STF(c0, m, c1);
    }

    public static STF EstimateLinked(bool boost, double[] median, double[] mad)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(median.Length, mad.Length);

        double lowClip = DefaultLowClip;
        double targetBackground = DefaultTargetMedian;
        if (boost)
        {
            lowClip *= LowClipBoostFactor;
            targetBackground *= TargetMedianBoostFactor;
        }

        int channels = median.Length;
        int invertedChannels = 0;
        for (int i = 0; i < channels; i++)
        {
            if (median[i] > 0.5)
                invertedChannels++;
        }

        double shadows = 0;
        double midtones = 0;
        double highlights = 0;

        if (invertedChannels < channels)
        {
            // Noninverted image
            for (int i = 0; i < channels; i++)
            {
                if (1 + mad[i] != 1)
                    shadows += median[i] + lowClip * mad[i];
                midtones += median[i];
            }

            shadows = double.Clamp(shadows / channels, 0, 1);
            midtones = MTF(targetBackground, midtones / channels - shadows);
            highlights = 1;
        }
        else
        {
            // Inveted image
            for (int i = 0; i < channels; i++)
            {
                if (1 + mad[i] != 1)
                {
                    shadows += median[i] - lowClip * mad[i];
                }
                else
                {
                    shadows += 1;
                }
                midtones += median[i];
            }

            highlights = double.Clamp(shadows / channels, 0, 1);
            midtones = MTF(highlights - midtones / channels, targetBackground);
            shadows = 0;
        }

        return new STF(shadows, midtones, highlights);
    }

    public static int GetLUTSizeInBits(STF stf)
    {
        int half = (byte.MaxValue >> 1) + 1;
        int bits = (int)double.Ceiling(double.Log2(half / (stf.Midtones * (stf.Highlights - stf.Shadows))));
        bits = int.Clamp(bits, 8, 24);
        return bits;
    }

    public static byte[] CreateLUT(STF stf)
    {
        int bits = GetLUTSizeInBits(stf);
        return CreateLUT(stf, bits);
    }

    public static byte[] CreateLUT(STF stf, int bits)
    {
        if (stf == Default)
        {
            var defaultLut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                defaultLut[i] = (byte)i;
            }
            return defaultLut;
        }

        bits = int.Clamp(bits, 8, 24);

        int lutSize = 1 << bits;
        var buffer = new byte[lutSize];

        bool hasClipping = stf.Flags.HasFlag(STFFlags.Clipping);
        bool hasDelta = stf.Flags.HasFlag(STFFlags.Delta);
        double invDelta = hasDelta ? 1.0f / (stf.Highlights - stf.Shadows) : 0.0f;

        var partitioner = Partitioner.Create(0, lutSize);

        if (hasClipping)
        {
            if (hasDelta)
            {
                Parallel.ForEach(partitioner, range =>
                {
                    int start = range.Item1;
                    int end = range.Item2;
                    Span<byte> lut = buffer;

                    for (int i = start; i < end; i++)
                    {
                        double value = (double)i / (lutSize - 1);
                        if (value <= stf.Shadows)
                            value = 0;
                        else if (value >= stf.Highlights)
                            value = 1;
                        else
                            value = double.Clamp((value - stf.Shadows) * invDelta, 0, 1);
                        lut[i] = (byte)double.Round(MTF(stf.Midtones, value) * byte.MaxValue);
                    }
                });
            }
            else
            {
                Parallel.ForEach(partitioner, range =>
                {
                    int start = range.Item1;
                    int end = range.Item2;
                    Span<byte> lut = buffer;

                    for (int i = start; i < end; i++)
                    {
                        lut[i] = byte.MaxValue;
                    }
                });
            }
        }
        else
        {
            Parallel.ForEach(partitioner, range =>
            {
                int start = range.Item1;
                int end = range.Item2;
                Span<byte> lut = buffer;

                for (int i = start; i < end; i++)
                {
                    double value = (double)i / (lutSize - 1);
                    lut[i] = (byte)double.Round(MTF(stf.Midtones, value) * byte.MaxValue);
                }
            });
        }

        return buffer;
    }
}