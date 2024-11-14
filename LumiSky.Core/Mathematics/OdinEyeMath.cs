using System.Numerics;
using System.Runtime.CompilerServices;

namespace LumiSky.Core.Mathematics;

public static class LumiSkyMath
{
    public const double SiderealDayInHours = 23.9344696;
    public const double Deg2Rad = Math.PI / 180;
    public const double Rad2Deg = 180 / Math.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Min3<T>(T a, T b, T c)
        where T : INumber<T>
    {
        T min = a;
        if (b < min) min = b;
        if (c < min) min = c;
        return min;
    }

    /// <summary>
    /// Find the maximum of 4 numbers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Max4<T>(T a, T b, T c, T d)
        where T : INumber<T>
    {
        T max = a;
        if (b > max) max = b;
        if (c > max) max = c;
        if (d > max) max = d;
        return max;
    }
}
