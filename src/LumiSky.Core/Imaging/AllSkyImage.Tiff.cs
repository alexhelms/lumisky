using LumiSky.Core.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Imaging;

public partial class AllSkyImage
{
    public static AllSkyImage FromTiff(string filename)
    {
        var info = Image.Identify(filename);
        if (info.Metadata.DecodedImageFormat != TiffFormat.Instance)
            throw new ArgumentException($"File is not a tiff: {filename}");

        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
        return FromTiff(fs);
    }

    public static AllSkyImage FromTiff(Stream stream)
    {
        using Image<L16> tiff = Image.Load<L16>(stream);

        if (tiff.Metadata.DecodedImageFormat != TiffFormat.Instance)
            throw new ArgumentException($"Stream is not a tiff");

        if (tiff.PixelType.BitsPerPixel != 16)
            throw new ArgumentException("Only 16-bit tiff images are supported");

        var data = new Memory2D<ushort>(tiff.Width, tiff.Height);
        Span<byte> dataAsBytes = MemoryMarshal.Cast<ushort, byte>(data.GetSpan());
        tiff.CopyPixelDataTo(dataAsBytes);

        return new AllSkyImage(data);
    }
}
