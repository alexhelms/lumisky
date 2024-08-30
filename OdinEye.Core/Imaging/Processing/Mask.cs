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

        using var mask = new Mat(mat.Size, DepthType.Cv32F, 3);
        CvInvoke.Circle(mask, new System.Drawing.Point(x, y), diameter / 2, new MCvScalar(1.0, 1.0, 1.0), -1, LineType.AntiAlias);

        if (blur > 0)
        {
            // blur must be an odd number
            if (blur % 2 != 1) blur++;
            Math.Clamp(blur, 1, MaxBlurSize);
            CvInvoke.Blur(mask, mask, new(blur, blur), new(-1, -1));
        }

        unsafe
        {
            var maskSpan = mask.GetSpan<float>();
            var matSpan = mat.GetSpan<byte>();
            for (int i = 0; i < matSpan.Length; i++)
            {
                matSpan[i] = (byte)(matSpan[i] * maskSpan[i] + matSpan[i] * (1.0f - maskSpan[i]));
            }
        }

        // Why is this different on linux!
        //CvInvoke.Multiply(mask, mat, mat, 1/255.0, DepthType.Cv8U);
    }
}
