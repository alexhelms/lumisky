using System.Diagnostics;

namespace LumiSky.Core.Utilities;

public sealed class Benchmark : IDisposable
{
    private readonly long _start;
    private readonly Action<TimeSpan> _action;

    public Benchmark(Action<TimeSpan> action)
    {
        _action = action;
        _start = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        var stop = Stopwatch.GetTimestamp();
        _action(Stopwatch.GetElapsedTime(_start, stop));
    }

    public static Benchmark Start(Action<TimeSpan> action) => new(action);
}
