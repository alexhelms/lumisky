using LumiSky.Core.Imaging;

namespace LumiSky.Core.Devices;

internal static class RaspiCamUtils
{
    public static ExposureParameters ClampExposureParameters(ExposureParameters parameters)
    {
        var gain = int.Clamp(parameters.Gain, 1, int.MaxValue);
        var offset = parameters.Offset;
        var binning = parameters.Binning;
        var shutter = parameters.Duration.TotalMicroseconds;

        if (binning != 1)
            Log.Warning("Raspi camera binning is not implemented");

        if (offset != 0)
            Log.Warning("Raspi camera offset is not supported");

        return new ExposureParameters
        {
            Gain = gain,
            Offset = offset,
            Duration = TimeSpan.FromMicroseconds(shutter),
            Binning = binning,
        };
    }

    public static string CreateArgs(ExposureParameters parameters, string outputName)
    {
        // Output doesn't matter, an additional DNG will be created with the same name.
        string[] argList = [
            "--immediate",
            "--nopreview",
            "--raw",
            "--denoise off",
            $"--gain {parameters.Gain}",
            $"--shutter {(int)parameters.Duration.TotalMicroseconds}",
            "--awbgains 1,1",
            $"--output {outputName}.jpg",
        ];

        string args = string.Join(" ", argList);
        return args;
    }

    public static BayerPattern GetBayerPatternFromOutput(string output)
    {
        // Successful output puts text in stderr, NOT stdout.
        var stderrLines = output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        BayerPattern bayerPattern = BayerPattern.RGGB;
        var bayerLine = stderrLines[^1];
        if (bayerLine.StartsWith("Bayer format is"))
        {
            // Text is like: Bayer format is BGGR-16-PISP
            var bayerStr = bayerLine.Split(' ')[^1].Split('-').First();
            bayerPattern = Enum.Parse<BayerPattern>(bayerStr);
        }
        else
        {
            Log.Warning("Could not parse raspi camera bayer pattern, assuming {BayerPattern}", bayerPattern);
        }

        return bayerPattern;
    }
}
