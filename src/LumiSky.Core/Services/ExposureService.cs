using MathNet.Numerics.Statistics;
using LumiSky.Core.Mathematics;
using LumiSky.Core.Profile;
using Microsoft.Extensions.Caching.Memory;

namespace LumiSky.Core.Services;

public class ExposureService
{
    private static readonly TimeSpan DefaultFirstExposure = TimeSpan.FromSeconds(3);

    private readonly IProfileProvider _profile;
    private readonly SunService _sunService;
    private readonly IMemoryCache _memoryCache;

    // TODO: A visualization of the queue on the gui as well as the coefficients for the fits
    private Queue<double> ElectronQueue { get; } = [];
    private int WindowSize { get; } = 15;
    private TimeSpan ExposureSecNext { get; set; } = DefaultFirstExposure;

    public double[] PredictionCoefficients { get; set; } = [];
    public IReadOnlyList<double> PredictionData => ElectronQueue.ToList();
    public event EventHandler? DataChanged;

    public ExposureService(
        IProfileProvider profile,
        SunService sunService,
        IMemoryCache memoryCache)
    {
        _profile = profile;
        _sunService = sunService;
        _memoryCache = memoryCache;
    }

    public TimeSpan GetNextExposure()
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        Log.Information("Next Exposure is {Exposure:0.000000} sec", ExposureSecNext.TotalSeconds);
        return ExposureSecNext;
    }

    public void AddMostRecentStatistics(TimeSpan exposure, double median, int gain)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);

        bool isDay = _sunService.IsDaytime();
        var maxExposureSeconds = Math.Min(_profile.Current.Capture.MaxExposureDuration.TotalSeconds,
            _profile.Current.Capture.CaptureInterval.TotalSeconds);
        var maxExposure = TimeSpan.FromSeconds(maxExposureSeconds);
        double bias = isDay
            ? _profile.Current.Camera.DaytimeBiasG
            : _profile.Current.Camera.NighttimeBiasG;
        var lowerThresh = bias * 1.1;
        var upperThresh = 0.9;

        if (median < lowerThresh)
        {
            // Clipped, do not store
            // Set the next exposure to double the supplied exposure
            // in hopes of getting an image that is not clipped.

            Log.Information("Image clipped with median={Median:F6}, doubling exposure", median);
            ExposureSecNext = exposure * 2;
            if (ExposureSecNext > maxExposure)
                ExposureSecNext = maxExposure;
            return;
        }
        else if (median > upperThresh)
        {
            // Clipped, do not store
            // Set the next exposure to quarter the supplied exposure
            // in hopes of getting an image that is not clipped.

            Log.Information("Image clipped with median={Median:F6}, quartering exposure", median);
            ExposureSecNext = exposure / 4;
            return;
        }

        double targetMedian = _profile.Current.Camera.TargetMedian / ushort.MaxValue;
        double conversionGain = isDay
            ? _profile.Current.Camera.DaytimeElectronGain
            : _profile.Current.Camera.NighttimeElectronGain;

        // Ensure positive, nonzero, negative doesn't make sense
        conversionGain = Math.Clamp(conversionGain, 0.001, double.MaxValue);

        // Ensure positive and non-zero
        double exposureSec = Math.Clamp(exposure.TotalSeconds, 1e-6, double.MaxValue);

        // Ensure positive
        double ePerSec = (median - bias) * conversionGain / exposureSec;
        ePerSec *= ushort.MaxValue; // denormalize
        ePerSec = Math.Clamp(ePerSec, 1e-6, double.MaxValue);

        // Maintain window size
        ElectronQueue.Enqueue(ePerSec);
        while (ElectronQueue.Count > WindowSize)
            ElectronQueue.Dequeue();

        double ePerSecNext = ePerSec;

        // Min 3 points for regression
        if (ElectronQueue.Count > 3)
        {
            if (ElectronQueue.Count < 7)
            {
                // Linear
                ePerSecNext = PredictNextElectronLinear();
            }
            else
            {
                // RANSAC
                ePerSecNext = PredictNextElectronRansac();
            }
        }

        // protect exposure explosion
        var twicePrevious = 4.0 * ePerSec;
        var halfPrevious = 0.25 * ePerSec;
        ePerSecNext = Math.Clamp(ePerSecNext, halfPrevious, twicePrevious);

        double exposureNextSec = targetMedian * conversionGain / ePerSecNext;
        exposureNextSec *= ushort.MaxValue; // denormalize

        // min/max clamp to prevent runaway
        exposureNextSec = Math.Clamp(exposureNextSec, 1e-6, maxExposure.TotalSeconds);

        ExposureSecNext = TimeSpan.FromSeconds(exposureNextSec);

        // Cache the next exposure duration so restarting the job pipeline can be avoid the auto exposure startup time.
        _memoryCache.Set(CacheKeys.NextExposure, ExposureSecNext, DateTimeOffset.UtcNow.AddMinutes(3));

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private double PredictNextElectronLinear()
    {
        double[] x = Enumerable.Range(0, ElectronQueue.Count).Select(x => (double)x).ToArray();
        double[] y = ElectronQueue.ToArray();
        double[] w = new double[x.Length];
        
        for (int i = 0; i < w.Length; i++)
        {
            // W = log(x + 1.5) / log(n + 1.5)
            // where n is number of weights
            // - Oldest point has weight of ~0.117
            // - Newest point has weight of 1.0
            w[i] = Math.Log(i + 1.5) / Math.Log(w.Length + 1.5);
        }

        double[] coeffs = MathNet.Numerics.Fit.PolynomialWeighted(x, y, w, 1);

        PredictionCoefficients = coeffs;
        return MathNet.Numerics.Polynomial.Evaluate(x.Length, coeffs);
    }

    private double PredictNextElectronRansac()
    {
        double[] x = Enumerable.Range(0, ElectronQueue.Count).Select(x => (double)x).ToArray();
        double[] y = ElectronQueue.ToArray();
        double[] w = new double[x.Length];

        var coeffs = RansacPolynomialRegression.Fit(x, y, 1, new()
        {
            // Inlier threshold tied to Y values because at night the values are very small, <50, but
            // during the day they are very large, >20e6.
            InlierThreshold = Math.Log(y.Mean() + 1),
            MaxIterations = 2000,
            MinInliers = 2
        });

        PredictionCoefficients = coeffs;
        return MathNet.Numerics.Polynomial.Evaluate(x.Length, coeffs);
    }
}
