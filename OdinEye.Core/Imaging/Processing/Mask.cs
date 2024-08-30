using Emgu.CV;
using Emgu.CV.CvEnum;

using Emgu.CV.Structure;

namespace OdinEye.Core.Imaging.Processing;

public static class Mask
{
    public static void Circle(Mat mat, int x, int y, int diameter, int blur = 5)
    {
        const int MaxBlurSize = 51;
        Math.Clamp(diameter, 0, int.MaxValue);

        using var mask = new Mat(mat.Size, DepthType.Cv8U, 3);
        mask.SetTo(new MCvScalar(0, 0, 0));
        CvInvoke.Circle(mask, new System.Drawing.Point(x, y), diameter / 2, new MCvScalar(255, 255, 255), -1, LineType.AntiAlias);

        if (blur > 0)
        {
            // blur must be an odd number
            if (blur % 2 != 1) blur++;
            Math.Clamp(blur, 1, MaxBlurSize);
            CvInvoke.Blur(mask, mask, new(blur, blur), new(-1, -1));
        }

        CvInvoke.Multiply(mask, mat, mat, 1/255.0, DepthType.Cv8U);
    }
}
