using MathNet.Numerics;
using OdinEye.Core.Extensions;

namespace OdinEye.Core.Mathematics;

public record RansacPolynomialRegressionOptions
{
    public static RansacPolynomialRegressionOptions Default { get; } = new();

    public int MaxIterations { get; init; } = 1000;

    public int MinInliers { get; init; } = 2;

    public double InlierThreshold { get; init; } = 0.1;
}

public static class RansacPolynomialRegression
{
    public static double[] Fit(double[] x, double[] y, int degree, RansacPolynomialRegressionOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(x.Length, y.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(degree, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(x.Length, degree + 1);  // degree of freedom check

        int numSamples = degree + 1;
        double bestRmse = double.MaxValue;
        double[] bestCoeffs = [];
        double[] xx = new double[numSamples];
        double[] yy = new double[numSamples];
        double[] residuals = new double[x.Length];

        for (int i = 0; i < options.MaxIterations; i++)
        {
            int numInliers = 0;

            // Select our random samples
            int[] sampleIndices = Random.Shared.RandomSample(x.Length, numSamples);
            for (int j = 0; j < numSamples; j++)
            {
                xx[j] = x[sampleIndices[j]];
                yy[j] = y[sampleIndices[j]];
            }

            double[] coeffs = MathNet.Numerics.Fit.Polynomial(xx, yy, degree);

            // Compute residuals
            for (int j = 0; j < x.Length; j++)
            {
                double refValue = y[j];
                double predValue = Polynomial.Evaluate(x[j], coeffs);
                residuals[j] = Math.Sqrt((refValue - predValue) * (refValue - predValue));

                if (residuals[j] < options.InlierThreshold)
                {
                    numInliers++;
                }
            }

            if (numInliers < options.MinInliers)
                continue;

            double residualsSum = residuals.Sum();
            double rmse = Math.Sqrt(residualsSum / residuals.Length);

            if (rmse < bestRmse)
            {
                bestRmse = rmse;
                bestCoeffs = coeffs;
            }
        }

        // last element is highest exponent
        // p0 + p1*x^1 + p2*x^2 ...
        return bestCoeffs;
    }
}
