using Emgu.CV;
using Emgu.CV.CvEnum;
using LumiSky.Core.Primitives;

namespace LumiSky.Core.Imaging.Processing;

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

        using var fisheyeImage = image.Clone();
        Rotate(fisheyeImage, angle + 90);

        var fisheyeBounds = new Rectangle(0, 0, image.Cols, image.Rows);

        // Width must be even number for ffmpeg
        var equirectWidth = (int)(2 * Math.PI * radius + 0.5);
        if (equirectWidth % 2 == 1)
            equirectWidth--;

        // Height must be even number for ffmpeg
        var equirectHeight = (int)(Math.PI * radius + 0.5);
        if (equirectHeight % 2 == 1)
            equirectHeight--;

        int centerX = (int)(fisheyeBounds.Width / 2 + xOffset);
        int centerY = (int)(fisheyeBounds.Height / 2 + yOffset);
        
        var equirectBounds = new Rectangle(0, 0, equirectWidth, equirectHeight);
        var panoImage = new Mat(equirectBounds.Height, equirectBounds.Width, DepthType.Cv8U, 3);

        var op = new PanoramaOperation(
            fisheyeImage.DataPointer,
            fisheyeImage.Step,
            panoImage.DataPointer,
            panoImage.Step,
            fisheyeBounds,
            equirectBounds,
            centerX,
            centerY,
            radius);
        op.Run();

        return panoImage;
    }

    private class PanoramaOperation : IRowIntervalOperation
    {
        const int Channels = 3;

        private nint fisheyePtr;
        private int fisheyeStride;
        private nint equirectPtr;
        private int equirectStride;
        private Rectangle fisheyeBounds;
        private Rectangle equirectBounds;
        private int centerX;
        private int centerY;
        private double radius;

        public PanoramaOperation(nint fisheyePtr, int fisheyeStride, nint equirectPtr, int equirectStride,
            Rectangle fisheyeBounds, Rectangle equirectBounds, int centerX, int centerY, double radius)
        {
            this.fisheyePtr = fisheyePtr;
            this.fisheyeStride = fisheyeStride;
            this.equirectPtr = equirectPtr;
            this.equirectStride = equirectStride;
            this.fisheyeBounds = fisheyeBounds;
            this.equirectBounds = equirectBounds;
            this.centerX = centerX;
            this.centerY = centerY;
            this.radius = radius;
        }

        public void Run()
        {
            ParallelRowIterator.IterateRowIntervals(equirectBounds, this);
        }

        public unsafe void Invoke(in RowInterval rows)
        {
            int fisheyeLength = fisheyeBounds.Height * fisheyeStride;
            int equirectLength = equirectBounds.Height * equirectStride;
            var fisheyeSpan = new Span<byte>((void*)fisheyePtr, fisheyeLength);
            var equirectSpan = new Span<byte>((void*)equirectPtr, equirectLength);

            for (int y = rows.Top; y < rows.Bottom; y++)
            {
                double r0 = radius * y / equirectBounds.Height;

                for (int x = rows.Left; x < rows.Right; x++)
                {
                    double theta = 2 * Math.PI * x / equirectBounds.Width;
                    double xx = r0 * Math.Cos(theta) + centerX;
                    double yy = r0 * Math.Sin(theta) + centerY;
                    int ix = (int)(xx + 0.5);
                    int iy = (int)(yy + 0.5);
                    if (xx >= 0 && ix < fisheyeBounds.Width && yy >= 0 && iy < fisheyeBounds.Height)
                    {
                        int fisheyeOffset = (fisheyeStride * iy) + (ix * Channels);
                        int equirectOffset = (equirectStride * y) + (x * Channels);
                        equirectSpan[equirectOffset + 0] = fisheyeSpan[fisheyeOffset + 0];
                        equirectSpan[equirectOffset + 1] = fisheyeSpan[fisheyeOffset + 1];
                        equirectSpan[equirectOffset + 2] = fisheyeSpan[fisheyeOffset + 2];
                    }
                }
            }
        }
    }
}
