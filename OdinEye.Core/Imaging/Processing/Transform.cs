using Emgu.CV;
using Emgu.CV.CvEnum;

namespace OdinEye.Core.Imaging.Processing;

public static class Transform
{
    public static void Rotate(Mat mat, double angle)
    {
        using var rotationMatrix = new Mat();
        CvInvoke.GetRotationMatrix2D(new(mat.Cols / 2, mat.Rows/ 2), angle, 1.0, rotationMatrix);
        CvInvoke.WarpAffine(mat, mat, rotationMatrix, mat.Size, Inter.Lanczos4);
    }

    public static void FlipHorizontal(Mat mat)
    {
        CvInvoke.Flip(mat, mat, FlipType.Horizontal);
    }

    public static void FlipVertical(Mat mat)
    {
        CvInvoke.Flip(mat, mat, FlipType.Vertical);
    }

    public static Mat Panorama(Mat image, double xOffset, double yOffset, double radius, double angle)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(image.NumberOfChannels, 3);
        if (image.Depth != DepthType.Cv8U)
            throw new ArgumentOutOfRangeException(nameof(image.Depth));

        using var imageClone = image.Clone();
        Rotate(imageClone, angle);

        int channels = imageClone.NumberOfChannels;
        int width = imageClone.Cols;
        int height = imageClone.Rows;
        int centerX = (int)(width / 2 + xOffset);
        int centerY = (int)(height / 2 + yOffset);

        int w = (int)(2 * Math.PI * radius + 0.5);
        int h = (int)(Math.PI * radius + 0.5);

        var panoImage = new Mat(h, w, DepthType.Cv8U, 3);

        unsafe
        {
            var srcSpan = imageClone.GetSpan<byte>();
            var dstSpan = panoImage.GetSpan<byte>();

            for (int y = 0; y < h; y++)
            {
                double r0 = radius * y / h;

                for (int x = 0; x < w; x++)
                {
                    double theta = 2 * Math.PI * x / w;
                    double xx = r0 * Math.Cos(theta) + centerX;
                    double yy = r0 * Math.Sin(theta) + centerY;
                    int ix = (int)(xx + 0.5);
                    int iy = (int)(yy + 0.5);
                    if (xx > 0 && ix < width && yy > 0 && iy < height)
                    {
                        int srcOffset = (imageClone.Step * iy) + (ix * channels);
                        int dstOffset = (panoImage.Step * y) + (x * channels);
                        dstSpan[dstOffset + 0] = srcSpan[srcOffset + 0];
                        dstSpan[dstOffset + 1] = srcSpan[srcOffset + 1];
                        dstSpan[dstOffset + 2] = srcSpan[srcOffset + 2];
                    }
                }
            }
        }

        return panoImage;
    }
}