using System.Numerics;
using System.Runtime.CompilerServices;

namespace OdinEye.Core.Mathematics;

public static class OdinEyeMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Min3<T>(T a, T b, T c)
        where T : INumber<T>
    {
        T min = a;
        if (b < min) min = b;
        if (c < min) min = c;
        return min;
    }
}
