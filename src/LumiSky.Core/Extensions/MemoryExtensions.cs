using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Memory;

public static class MemoryExtensions
{
    private static DepthType GetDepthType<T>()
        where T : unmanaged, INumber<T>
    {
        if (typeof(T) == typeof(byte))
            return DepthType.Cv8U;
        else if (typeof(T) == typeof(sbyte))
            return DepthType.Cv8S;
        else if (typeof(T) == typeof(ushort))
            return DepthType.Cv16U;
        else if (typeof(T) == typeof(short))
            return DepthType.Cv16S;
        else if (typeof(T) == typeof(int))
            return DepthType.Cv32S;
        else if (typeof(T) == typeof(float))
            return DepthType.Cv32F;
        else if (typeof(T) == typeof(double))
            return DepthType.Cv64F;

        throw new NotSupportedException();
    }

    public static unsafe Mat ToMat<T>(this Memory2D<T> memory)
        where T : unmanaged, INumber<T>
    {
        DepthType depthType = GetDepthType<T>();
        
        // Memory2D is backed by unmanaged memory so it can't get moved
        ref var data = ref MemoryMarshal.GetReference(memory.GetSpan());
        return new Mat(memory.Height, memory.Width, depthType, 1, (nint)Unsafe.AsPointer(ref data), memory.Width * sizeof(T));
    }

    public static unsafe Mat ToMat<T>(this Memory3D<T> memory)
        where T : unmanaged, INumber<T>
    {
        DepthType depthType = GetDepthType<T>();

        if (memory.Channels == 1)
        {
            // Memory2D is backed by unmanaged memory so it can't get moved
            ref var data = ref MemoryMarshal.GetReference(memory.GetSpan());
            return new Mat(memory.Height, memory.Width, depthType, memory.Channels, (nint)Unsafe.AsPointer(ref data), memory.Width * sizeof(T));
        }
        else
        {
            // LumiSky Memory3D and OpenCV have different memory layout so we must convert.
            //
            // LumiSky: RRR | GGG | BBB
            //  OpenCV: BGR | BGR | BGR

            using var mats = new VectorOfMat();
            for (int c = 0; c < memory.Channels; c++)
            {
                ref var data = ref MemoryMarshal.GetReference(memory.GetSpan(c));
                var mat = new Mat(
                    memory.Height,
                    memory.Width,
                    depthType,
                    1,
                    (nint) Unsafe.AsPointer(ref data),
                    memory.Width * sizeof(float));
                mats.Push(mat);
            }

            // The Mat returned has allocated it's own memory and is no longer linked to the original AllSkyImage.
            // Multi-channel operations have to copy the Mat data back to LumiSky.
            Mat output = new();
            CvInvoke.Merge(mats, output);

            return output;
        }
    }

    public static void ToBlob(this Mat mat, string filename)
    {
        Span<byte> span = mat.GetSpan<byte>();
        using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
        {
            fs.Write(span);
        }
    }

    public static void FromBlob(this Mat mat, string filename)
    {
        Span<byte> span = mat.GetSpan<byte>();
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            fs.ReadExactly(span);
        }
    }
}
