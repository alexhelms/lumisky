using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using OdinEye.Core.Memory;
using OdinEye.Core.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OdinEye.Core.Imaging;

public static class AllSkyImageExtensions
{
    /// <summary>
    /// Convert <paramref name="image"/> to an OpenCV Mat.
    /// 
    /// The underlying memory is linked for single channel images only.
    ///
    /// For multi-channel images, data must be copied to and from OdinEye to OpenCV
    /// because the memory layout is different. For this reason, you must always
    /// call <see cref="FromMat(AllSkyImage, Mat)"/> to ensure the data is copied
    /// back from OpenCV.
    /// </summary>
    public static Mat ToMat(this AllSkyImage image)
    {
        return image.Data.ToMat();
    }

    public static Mat To8bitMat(this AllSkyImage image)
    {
        if (image.Channels == 1)
        {
            using var data = new Memory2D<byte>(image.Width, image.Height);
            ImagingUtil.NormalizedFloatToUInt8(image.Data.GetSpan(), data.GetSpan());

            unsafe
            {
                var mat = new Mat(image.Height, image.Width, DepthType.Cv8U, 1);
                var matSpan = new Span<byte>((void*) mat.DataPointer, (int) mat.Total);
                var srcSpan = data.GetSpan();
                srcSpan.CopyTo(matSpan);
                return mat;
            }
        }
        else
        {
            // OdinEye Memory3D and OpenCV have different memory layout so we must convert.
            // OpenCV multi-channel Mats will never point to the OdinEye memory.
            //
            // OdinEye: RRR | GGG | BBB
            //  OpenCV: BGR | BGR | BGR

            var mat = new Mat(image.Height, image.Width, DepthType.Cv8U, image.Channels);
            using var mats = new VectorOfMat();
            CvInvoke.Split(mat, mats);

            for (int c = 0; c < image.Channels; c++)
            {
                // Each "element" is really C elements where C is the number of channels
                int elementSize = mat.ElementSize / mat.NumberOfChannels;
                int numElements =  (int) (mats[c].Total * mats[c].NumberOfChannels);

                unsafe
                {
                    var matSpan = new Span<byte>((void*) mats[c].DataPointer, numElements);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = image.Data.GetRowSpan(y, c);
                        var dstSpan = matSpan.Slice(y * mats[c].Width, mats[c].Width);
                        ImagingUtil.NormalizedFloatToUInt8(srcSpan, dstSpan);
                    }
                }
            }

            CvInvoke.Merge(mats, mat);
            return mat;
        }
        
    }

    /// <summary>
    /// Copy OpenCV Mat image data to <paramref name="image"/>.
    ///
    /// If the image is single channel, the underlying memory is linked
    /// and this function will not perform a copy.
    ///
    /// For multichannel images, data must be copied to and from OdinEye to OpenCV
    /// because the memory layout is different.
    /// </summary>
    public static unsafe void FromMat(this AllSkyImage image, Mat mat)
    {
        if (image.Size != mat.Size) throw new ArgumentException("image and mat are not the same size");
        if (image.Channels != mat.NumberOfChannels) throw new ArgumentException("The image and mat do not have the same number of channels");

        if (image.Channels == 1)
        {
            nint odineyeData = (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(image.Data.GetSpan()));
            nint opencvData = mat.DataPointer;

            if (odineyeData == opencvData)
            {
                // Nothing to do, opencv is modifying the OdinEye image directly.
                return;
            }
            else
            {
                if (mat.ElementSize == sizeof(float))
                {
                    var matSpan = new Span<float>((void*) mat.DataPointer, (int) mat.Total);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mat.Width, mat.Width);
                        var dstSpan = image.Data.GetRowSpan(y);
                        srcSpan.CopyTo(dstSpan);
                    }
                }
                else if (mat.ElementSize == sizeof(ushort))
                {
                    var matSpan = new Span<ushort>((void*) mat.DataPointer, (int) mat.Total);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mat.Width, mat.Width);
                        var dstSpan = image.Data.GetRowSpan(y);
                        ImagingUtil.UInt16ToNormalizedFloat(srcSpan, dstSpan);
                    }
                }
                else if (mat.ElementSize == sizeof(byte))
                {
                    var matSpan = new Span<byte>((void*) mat.DataPointer, (int) mat.Total);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mat.Width, mat.Width);
                        var dstSpan = image.Data.GetRowSpan(y);
                        ImagingUtil.UInt8ToNormalizedFloat(srcSpan, dstSpan);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
        else
        {
            // OdinEye Memory3D and OpenCV have different memory layout so we must convert.
            // OpenCV multi-channel Mats will never point to the OdinEye memory.
            //
            // OdinEye: RRR | GGG | BBB
            //  OpenCV: BGR | BGR | BGR

            using var mats = new VectorOfMat();
            CvInvoke.Split(mat, mats);

            for (int c = 0; c < image.Channels; c++)
            {
                // Each "element" is really C elements where C is the number of channels
                int elementSize = mat.ElementSize / mat.NumberOfChannels;
                int numElements =  (int) (mats[c].Total * mats[c].NumberOfChannels);

                if (elementSize == sizeof(float))
                {
                    var matSpan = new Span<float>((void*) mats[c].DataPointer, numElements);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mats[c].Width, mats[c].Width);
                        var dstSpan = image.Data.GetRowSpan(y, c);
                        srcSpan.CopyTo(dstSpan);
                    }
                }
                else if (elementSize == sizeof(ushort))
                {
                    var matSpan = new Span<ushort>((void*) mats[c].DataPointer, numElements);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mats[c].Width, mats[c].Width);
                        var dstSpan = image.Data.GetRowSpan(y, c);
                        ImagingUtil.UInt16ToNormalizedFloat(srcSpan, dstSpan);
                    }
                }
                else if (elementSize == sizeof(byte))
                {
                    var matSpan = new Span<byte>((void*) mats[c].DataPointer, numElements);
                    for (int y = 0; y < image.Height; y++)
                    {
                        // Row by row because opencv may have padding at the end of the row
                        // and OdinEye is a contiguous block with no padding.
                        var srcSpan = matSpan.Slice(y * mats[c].Width, mats[c].Width);
                        var dstSpan = image.Data.GetRowSpan(y, c);
                        ImagingUtil.UInt8ToNormalizedFloat(srcSpan, dstSpan);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
