using OdinEye.Core.IO.Fits;
using OdinEye.Core.Memory;
using OdinEye.Core.Utilities;

namespace OdinEye.Core.Imaging;

public enum ImageOutputType
{
    Float32,
    UInt8,
    UInt16,
}

public partial class AllSkyImage
{
    public static AllSkyImage FromFits(string filename)
    {
        using var fits = new FitsFile(filename, FitsFile.IoMode.Read);
        if (fits.Channels != 1)
            throw new NotSupportedException("Only single channel FITS files are supported.");

        AllSkyImage image;

        var pixelType = fits.ReadPixelType();
        if (pixelType == typeof(ushort)) // most common
        {
            image = fits.Channels == 1 ? ReadUInt16SingleChannel(fits) : ReadUInt16MultiChannel(fits);
        }
        else if (pixelType == typeof(float))
        {
            image = fits.Channels == 1 ? ReadFloatSingleChannel(fits) : ReadFloatMultiChannel(fits);
        }
        else if (pixelType == typeof(byte))
        {
            image = fits.Channels == 1 ? ReadUInt8SingleChannel(fits) : ReadUInt8MultiChannel(fits);
        }
        else
        {
            throw new NotSupportedException($"{pixelType.Name} images are not supported");
        }

        // Compute statistics
        for (int c = 0; c < image.Channels; c++)
            new StatisticsOperation(image, c).Run();

        var header = fits.ReadHeader();
        var metadata = header.ToImageMetadata();
        image.Metadata.Merge(metadata);

        return image;
    }

    public void SaveAsFits(string filename, ImageOutputType type = ImageOutputType.Float32, bool overwrite = false)
    {
        Action<FitsFile> action = (type, Channels) switch
        {
            (ImageOutputType.Float32, _) => SaveFloat32,
            (ImageOutputType.UInt8, 1) => SaveUInt8Single,
            (ImageOutputType.UInt8, >1) => SaveUInt8Multi,
            (ImageOutputType.UInt16, 1) => SaveUInt16Single,
            (ImageOutputType.UInt16, >1) => SaveUInt16Multi,
            _ => throw new NotSupportedException(),
        };

        using var fits = new FitsFile(filename, FitsFile.IoMode.ReadWrite, overwrite);
        action(fits);

        var header = fits.ReadHeader();
        header.Items.AddRange(Metadata.ToFitsHeaderEntries());
        fits.WriteHeader(header);

        return;

        void SaveFloat32(FitsFile fits)
        {
            fits.Write(Data);
        }

        void SaveUInt8Single(FitsFile fits)
        {
            using var data = new Memory2D<byte>(Width, Height);
            ImagingUtil.NormalizedFloatToUInt8(Data.GetSpan(), data.GetSpan());
            fits.Write(data);
        }

        void SaveUInt8Multi(FitsFile fits)
        {
            using var data = new Memory3D<byte>(Width, Height, Channels);
            for (int c = 0; c < Channels; c++)
                ImagingUtil.NormalizedFloatToUInt8(Data.GetSpan(c), data.GetSpan(c));
            fits.Write(data);
        }

        void SaveUInt16Single(FitsFile fits)
        {
            using var data = new Memory2D<ushort>(Width, Height);
            ImagingUtil.NormalizedFloatToUInt16(Data.GetSpan(), data.GetSpan());
            fits.Write(data);
        }

        void SaveUInt16Multi(FitsFile fits)
        {
            using var data = new Memory3D<ushort>(Width, Height, Channels);
            for (int c = 0; c < Channels; c++)
                ImagingUtil.NormalizedFloatToUInt16(Data.GetSpan(c), data.GetSpan(c));
            fits.Write(data);
        }
    }

    private unsafe static AllSkyImage ReadUInt8SingleChannel(FitsFile fits)
    {
        using var data = fits.Read<byte>();
        var image = new AllSkyImage(data.Width, data.Height);
        var src = data.GetSpan();
        var dst = image.Data.GetSpan();
        ImagingUtil.UInt8ToNormalizedFloat(src, dst);
        return image;
    }

    private unsafe static AllSkyImage ReadUInt8MultiChannel(FitsFile fits)
    {
        using var data = fits.Read3D<byte>();
        var image = new AllSkyImage(data.Width, data.Height, data.Channels);

        for (int channel = 0; channel < data.Channels; channel++)
        {
            var src = data.GetSpan(channel);
            var dst = image.Data.GetSpan(channel);
            ImagingUtil.UInt8ToNormalizedFloat(src, dst);
        }

        return image;
    }

    private unsafe static AllSkyImage ReadUInt16SingleChannel(FitsFile fits)
    {
        using var data = fits.Read<ushort>();
        var image = new AllSkyImage(data.Width, data.Height);
        var src = data.GetSpan();
        var dst = image.Data.GetSpan();
        ImagingUtil.UInt16ToNormalizedFloat(src, dst);
        return image;
    }

    private unsafe static AllSkyImage ReadUInt16MultiChannel(FitsFile fits)
    {
        using var data = fits.Read3D<ushort>();
        var image = new AllSkyImage(data.Width, data.Height, data.Channels);

        for (int channel = 0; channel < data.Channels; channel++)
        {
            var src = data.GetSpan(channel);
            var dst = image.Data.GetSpan(channel);
            ImagingUtil.UInt16ToNormalizedFloat(src, dst);
        }

        return image;
    }

    private static AllSkyImage ReadFloatSingleChannel(FitsFile fits)
    {
        // do not dispose data, AllSkyImage assumes ownership.
        var data = fits.Read<float>();
        var image = new AllSkyImage(data);
        return image;
    }

    private static AllSkyImage ReadFloatMultiChannel(FitsFile fits)
    {
        using var data = fits.Read3D<float>();
        var image = new AllSkyImage(data.Clone());
        return image;
    }
}
