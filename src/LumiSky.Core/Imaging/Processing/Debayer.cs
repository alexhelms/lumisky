using Emgu.CV;
using Emgu.CV.CvEnum;
using LumiSky.Core.Imaging;
using LumiSky.Core.IO;
using LumiSky.Core.IO.Fits;
using LumiSky.Core.Memory;

namespace LumiSky.Core.Imaging.Processing;

public static class Debayer
{
    public static AllSkyImage FromFits(string fitsFilename)
    {
        var fileInfo = new FileInfo(fitsFilename);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("FITS file not found", fitsFilename);

        IDisposable? src = null;
        Mat? srcMat = null; ;
        DepthType depthType;

        try
        {
            using var fitsFile = new FitsFile(fitsFilename, FitsFile.IoMode.Read);

            var pixelType = fitsFile.ReadPixelType();
            if (pixelType == typeof(byte))
            {
                depthType = DepthType.Cv8U;
                using var srcData = fitsFile.Read<byte>();
                srcMat = srcData.ToMat();
                src = srcData;
            }
            else if (pixelType == typeof(ushort))
            {
                depthType = DepthType.Cv16U;
                var srcData = fitsFile.Read<ushort>();
                srcMat = srcData.ToMat();
                src = srcData;
            }
            else
            {
                throw new InvalidOperationException("Only 8-bit and 16-bit FITS can be debayered");
            }

            var header = fitsFile.ReadHeader();
            var bayerEntry = header.GetEntry<string>("BAYERPAT");
            var bayerPattern = bayerEntry?.Value ?? string.Empty;

            // If there is no bayer pattern, open and return the image
            if (string.IsNullOrWhiteSpace(bayerPattern))
                return AllSkyImage.FromFits(fitsFilename);

            var bayer = bayerPattern switch
            {
                "RGGB" => ColorConversion.BayerRggb2Rgb,
                "GRBG" => ColorConversion.BayerGrbg2Rgb,
                "BGGR" => ColorConversion.BayerBggr2Rgb,
                "GBRG" => ColorConversion.BayerGbrg2Rgb,
                _ => throw new NotImplementedException($"\"{bayerPattern}\" bayer pattern is not implemented"),
            };

            using var dstMat = new Mat(srcMat.Rows, srcMat.Cols, depthType, 3);
            CvInvoke.CvtColor(srcMat, dstMat, bayer);
            var image = new AllSkyImage(dstMat);
            var metadata = header.ToImageMetadata();
            image.Metadata.Merge(metadata);
            return image;
        }
        finally
        {
            srcMat?.Dispose();
            src?.Dispose();
        }
    }

    public static AllSkyImage FromImage(AllSkyImage image)
    {
        using var temporaryFile = new TemporaryFile();
        image.SaveAsFits(temporaryFile.Path, ImageOutputType.UInt16);
        var debayeredImage = FromFits(temporaryFile.Path);
        return debayeredImage;
    }
}
