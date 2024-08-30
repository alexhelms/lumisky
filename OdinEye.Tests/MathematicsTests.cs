using OdinEye.Core.Mathematics;

namespace OdinEye.Tests;

public class MathematicsTests
{
    [Fact]
    public void RansacPolynomial()
    {
        double[] x = [4.236547993389047, 6.458941130666561, 4.375872112626925, 8.917730007820797, 9.636627605010293, 3.8344151882577773, 7.917250380826646, 5.288949197529044, 5.680445610939323, 9.25596638292661, 0.7103605819788694, 0.8712929970154071, 0.2021839744032572, 8.32619845547938, 7.781567509498505];
        double[] y = [145.89046994400178, 514.6885196466181, 158.25183683347984, 1296.5931703589683, 1617.878583694894, 115.09739450950869, 910.8637630946092, 286.40412043184244, 328.4170948542821, 1438.7838406614528, 1.3489153124938102, 19.708174263143192, -0.8608581064522203, 1056.8485640333968, 879.9288344596539];
        double[] expectedCoeffs = [0.1427754110257098, -0.24333100234283636, 1.561915416059808, 1.6482641753070222];

        var coeffs = RansacPolynomialRegression.Fit(x, y, 3, new()
        {
            InlierThreshold = 0.05,
            MinInliers = 4,
        });

        coeffs.Length.Should().Be(expectedCoeffs.Length);
        foreach (var (a, b) in coeffs.Zip(expectedCoeffs))
        {
            var absDiff = Math.Abs(a - b);
            absDiff.Should().BeLessThan(0.1);
        }
    }
}
