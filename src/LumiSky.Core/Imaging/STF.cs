using Emgu.CV.CvEnum;

namespace LumiSky.Core.Imaging;

/// <summary>
/// Screen transfer function for histogram transformations.
/// </summary>
public readonly struct STF : IEquatable<STF>
{
    public const double DefaultC0 = 0.0;
    public const double DefaultM = 0.5;
    public const double DefaultC1 = 1.0;

    /// Default shawdow clipping point in normalized MAD units from the median.
    const double DefaultLowClip = -2.8;

    /// Target background median for the transformed image, normalized [0, 1].
    const double DefaultTargetMedian = 0.25;

    /// Boost factor for low clipping point.
    const double LowClipBoostFactor = 0.75;

    /// Boost factor for target background median.
    const double TargetMedianBoostFactor = 2.0;

    /// <summary>
    /// Midtones point, normalized [0, 1].
    /// </summary>
    public double M { get; }

    /// <summary>
    /// Shadow clip point, normalized [0, 1].
    /// </summary>
    public double C0 { get; }

    /// <summary>
    /// Highlights clip point, normalized [0, 1].
    /// </summary>
    public double C1 { get; }

    public STF(double c0, double m, double c1)
    {
        C0 = c0;
        M = m;
        C1 = c1;
    }

    public STF(double m)
    {
        C0 = DefaultC0;
        M = m;
        C1 = DefaultC1;
    }

    public static STF Default = new STF(DefaultC0, DefaultM, DefaultC1);

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

    public readonly override string ToString() => $"STF {{{C0:F6}, {M:F6}, {C1:F6}}}";

    public override bool Equals(object? obj)
    {
        return obj is STF stf && Equals(stf);
    }

    public bool Equals(STF other)
    {
        return M == other.M &&
               C0 == other.C0 &&
               C1 == other.C1;
    }

    public override int GetHashCode() => HashCode.Combine(C0, M, C1);

    public static bool operator ==(STF left, STF right) => left.Equals(right);

    public static bool operator !=(STF left, STF right) => !(left == right);

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
            c0 = Math.Clamp(median + lowClip * mad, 0.0, 1.0);
            m = MTF(targetBackground, Math.Clamp(median - c0, 0, 1.0));
            c1 = 1.0;
        }
        else // Inverted
        {
            c1 = Math.Clamp(median - lowClip * mad, 0.0, 1.0);
            m = MTF(Math.Clamp(c1 - median, 0, 1.0), targetBackground);
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
        double c = 0;
        double m = 0;
        double c0 = 0;
        double c1 = 0;

        int numInverted = 0;
        for (int i = 0; i < channels; i++)
            if (median[i] > 0.5)
                numInverted++;

        if (numInverted < channels)
        {
            // Noninverted image
            for (int i = 0; i < channels; i++)
            {
                if (1 + median[i] != 1)
                    c += median[i] + lowClip * mad[i];
                m += median[i];
            }

            c0 = Math.Clamp(c / channels, 0.0, 1.0);
            m = MTF(targetBackground, Math.Clamp(m / channels - c0, 0.0, 1.0));
            c1 = 1.0;
        }
        else
        {
            // Inverted image
            for (int i = 0; i < channels; i++)
            {
                c += (1 + mad[i] != 1) ? median[i] - lowClip * mad[i] : 1.0;
                m += median[i];
            }

            c1 = Math.Clamp(c / channels, 0.0, 1.0);
            m = MTF(Math.Clamp(c1 - m / channels, 0.0, 1.0), targetBackground);
            c0 = 0.0;
        }

        return new STF(c0, m, c1);
    }
}
