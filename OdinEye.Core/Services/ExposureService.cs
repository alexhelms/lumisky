using OdinEye.Core.Mathematics;
using OdinEye.Core.Profile;

namespace OdinEye.Core.Services;

public class ExposureService
{
    private static readonly TimeSpan DefaultFirstExposure = TimeSpan.FromSeconds(3);

    private readonly IProfileProvider _profile;
    private readonly SunService _sunService;

    // TODO: A visualization of the queue on the gui as well as the coefficients for the fits
    private Queue<double> ElectronQueue { get; } = [];
    private int WindowSize { get; } = 30;
    private TimeSpan ExposureSecNext { get; set; } = DefaultFirstExposure;

    public double[] PredictionCoefficients { get; set; } = [];
    public IReadOnlyList<double> PredictionData => ElectronQueue.ToList();
    public event EventHandler? DataChanged;

    public ExposureService(
        IProfileProvider profile,
        SunService sunService)
    {
        _profile = profile;
        _sunService = sunService;
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

        if (median > 0.9)
        {
            // Clipped, do not store
            // Set the next exposure to half the supplied exposure
            // in hopes of getting an image that is not clipped.

            Log.Information("Image clipped with median={Median:F6}, quartering exposure", median);
            ExposureSecNext = exposure / 4;
            return;
        }

        // TODO: detect day/night switch

        bool isDay = _sunService.IsDaytime;
        TimeSpan maxExposure = _profile.Current.Capture.CaptureInterval;
        double targetMedian = _profile.Current.Camera.TargetMedian / ushort.MaxValue;
        double conversionGain = isDay
            ? _profile.Current.Camera.DaytimeElectronGain
            : _profile.Current.Camera.NighttimeElectronGain;
        double bias = isDay
            ? _profile.Current.Camera.DaytimeBias
            : _profile.Current.Camera.NighttimeBias;

        // Ensure positive, nonzero, negative doesn't make sense
        conversionGain = Math.Clamp(conversionGain, 0.001, double.MaxValue);

        // Ensure positive and non-zero
        double exposureSec = Math.Clamp(exposure.TotalSeconds, 1e-6, double.MaxValue);

        // Ensure positive
        double ePerSec = (median - bias) * conversionGain / exposureSec;
        ePerSec = Math.Clamp(ePerSec, 1e-6, double.MaxValue);

        // Maintain window size
        ElectronQueue.Enqueue(ePerSec);
        while (ElectronQueue.Count > WindowSize)
            ElectronQueue.Dequeue();

        double ePerSecNext = ePerSec;

        // Min 3 points for regression
        if (ElectronQueue.Count > 3)
        {
            if (ElectronQueue.Count < 10)
            {
                // Linear
                ePerSecNext = PredictNextElectronLinear();
            }
            else
            {
                // RANSAC parabola (degree 2)
                ePerSecNext = PredictNextElectronRansac();
            }
        }

        // protect exposure explosion
        var twicePrevious = 4.0 * ePerSec;
        var halfPrevious = 0.25 * ePerSec;
        ePerSecNext = Math.Clamp(ePerSecNext, halfPrevious, twicePrevious);

        double exposureNextSec = targetMedian * conversionGain / ePerSecNext;

        // min/max clamp to prevent runaway
        exposureNextSec = Math.Clamp(exposureNextSec, 1e-6, maxExposure.TotalSeconds);

        ExposureSecNext = TimeSpan.FromSeconds(exposureNextSec);
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

        var coeffs = RansacPolynomialRegression.Fit(x, y, 2, new()
        {
            InlierThreshold = 0.01,
            MaxIterations = 1000,
            MinInliers = 3
        });

        PredictionCoefficients = coeffs;
        return MathNet.Numerics.Polynomial.Evaluate(x.Length, coeffs);
    }
}
