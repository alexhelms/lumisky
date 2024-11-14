using System.Runtime.CompilerServices;

namespace System.Numerics;

public static class NumericExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult As<T, TResult>(this T value)
        where T : INumber<T>
        where TResult : INumber<TResult>
    {
        return TResult.CreateChecked(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double AsDouble<T>(this T value)
        where T : INumber<T>
    {
        return value.As<T, double>();
    }
}
