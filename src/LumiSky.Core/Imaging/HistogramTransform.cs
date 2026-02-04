using LumiSky.Core.Memory;
using System.Collections.Concurrent;

namespace LumiSky.Core.Imaging;

public class HistogramTransform
{
    public static void Apply(Memory2D<float> plane, STF stf)
    {
        var hasClipping = stf.Flags.HasFlag(STFFlags.Clipping);
        var hasDelta = stf.Flags.HasFlag(STFFlags.Delta);
        var invDelta = hasDelta
            ? (float)(1.0 / (stf.Highlights - stf.Shadows))
            : 1.0f;

        var partitioner = Partitioner.Create(0, plane.Length);

        if (hasClipping)
        {
            if (hasDelta)
            {
                Parallel.ForEach(partitioner, range =>
                {
                    int start = range.Item1;
                    int end = range.Item2;
                    var span = plane.GetSpan();

                    for (int i = start; i < end; i++)
                    {
                        float value = span[i];
                        value = (float)double.Clamp((value - stf.Shadows) * invDelta, 0, 1);
                        span[i] = (float)STF.MTF(stf.Midtones, value);
                    }
                });
            }
            else
            {
                Parallel.ForEach(partitioner, range =>
                {
                    int start = range.Item1;
                    int end = range.Item2;
                    var span = plane.GetSpan();

                    for (int i = start; i < end; i++)
                    {
                        span[i] = 1.0f;
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
                var span = plane.GetSpan();

                for (int i = start; i < end; i++)
                {
                    span[i] = (float)STF.MTF(stf.Midtones, span[i]);
                }
            });
        }
    }
}
