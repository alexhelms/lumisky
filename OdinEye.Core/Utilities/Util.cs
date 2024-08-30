using System.Runtime.CompilerServices;

namespace OdinEye.Core.Utilities;

public static class Util
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}