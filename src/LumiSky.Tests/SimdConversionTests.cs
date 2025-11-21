namespace LumiSky.Tests;

public class SimdConversionTests
{
    [Fact]
    public void UInt8ToFloat()
    {
        byte[] src = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        float[] dst = new float[256];
        LumiSky.Core.Utilities.ImagingUtil.UInt8ToFloat(src, dst);
        for (int i = 0; i < src.Length; i++)
        {
            Assert.Equal(src[i] / (float)byte.MaxValue, dst[i], 1e-7);
        }
    }

    [Fact]
    public void UInt16ToFloat()
    {
        ushort[] src = Enumerable.Range(0, 256).Select(i => (ushort)i).ToArray();
        float[] dst = new float[256];
        LumiSky.Core.Utilities.ImagingUtil.UInt16ToFloat(src, dst);
        for (int i = 0; i < src.Length; i++)
        {
            Assert.Equal(src[i] / (float)ushort.MaxValue, dst[i]);
        }
    }

    [Fact]
    public void FloatToUInt8()
    {
        float[] src = Enumerable.Range(0, 256).Select(i => i / 255f).ToArray();
        byte[] dst = new byte[256];
        LumiSky.Core.Utilities.ImagingUtil.FloatToUInt8(src, dst);
        for (int i = 0; i < src.Length; i++)
        {
            Assert.Equal((byte)(src[i] * byte.MaxValue), dst[i]);
        }
    }

    [Fact]
    public void FloatToUInt16()
    {
        float[] src = Enumerable.Range(0, 256).Select(i => i / 65535f).ToArray();
        ushort[] dst = new ushort[256];
        LumiSky.Core.Utilities.ImagingUtil.FloatToUInt16(src, dst);
        for (int i = 0; i < src.Length; i++)
        {
            Assert.Equal((ushort)(src[i] * ushort.MaxValue), dst[i]);
        }
    }
}
