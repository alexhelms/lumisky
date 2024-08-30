using OdinEye.Core.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OdinEye.Core.IO.Fits;

public partial class FitsFile : IDisposable
{
    private readonly nint _handle;
    private readonly IoMode _ioMode;
    private readonly string _filename;

    public enum IoMode
    {
        Read = 0,
        ReadWrite = 1,
    };

    // cfitsio handles these for us
    private static readonly string[] ForbiddenKeywords =
    {
        "SIMPLE",
        "BITPIX",
        "NAXIS",
        "NAXIS1",
        "NAXIS2",
        "EXTEND",
        "BZERO",
        "BSCALE",
    };

    // Default comments put in by cfitsio, not sure if these can be disabled?
    private static readonly string[] CfitsioDefaultComments =
    {
        @"FITS (Flexible Image Transport System) format is defined in 'Astronomy",
        @"and Astrophysics', volume 376, page 359; bibcode: 2001A&A...376..359H"
    };

    public FitsFile(string filename)
        : this(filename, IoMode.Read)
    {
    }

    public FitsFile(string filename, IoMode mode, bool overwrite = false)
    {
        _ioMode = mode;
        _filename = filename;

        Native.ErrorCode status;
        nint handle = 0;

        if (mode == IoMode.Read)
        {
            OpenFile();
        }
        else // Read/Write
        {
            if (File.Exists(filename))
            {
                if (overwrite)
                {
                    File.Delete(filename);
                    CreateFile();
                }
                else
                {
                    throw new Exception("File already exists");
                }
            }
            else
            {
                CreateFile();
            }
        }

        _handle = handle;

        void OpenFile()
        {
            Native.OpenDiskFile(ref handle, filename, (Native.IOMode)mode, out status);
            if (status != Native.ErrorCode.OK)
            {
                var errorMessage = Native.GetErrorMessage();
                if (status == Native.ErrorCode.FILE_NOT_OPENED)
                {
                    throw new FileNotFoundException(errorMessage, filename);
                }
                else
                {
                    throw new FitsException(status, errorMessage);
                }
            }
        }

        void CreateFile()
        {
            Native.CreateFile(ref handle, filename, out status);
            Native.ThrowIfNotOk(status);
        }
    }

    #region IDisposable

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // dispose managed resources
            }

            if (_handle != 0)
            {
                Native.CloseFile(_handle, out var status);
                Native.ThrowIfNotOk(status);
            }

            _disposed = true;
        }
    }

    ~FitsFile()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    public int Channels
    {
        get
        {
            if (_handle == 0)
                throw new ObjectDisposedException("FITS image already disposed");

            var naxes = new long[3];
            Native.ReadImageHeader(_handle, naxes.Length, out _, out _, out _, naxes, out _, out _, out _, out var status);
            Native.ThrowIfNotOk(status);

            return Math.Max(1, (int)naxes[2]);
        }
    }

    private Type ResolveHeaderDatatype(char dtype) => dtype switch
    {
        'C' => typeof(string),  // character
        'L' => typeof(bool),    // logical
        'I' => typeof(int),     // integer
        'F' => typeof(double),  // floating point
        _ => throw new ArgumentOutOfRangeException($"Unsupported FITS header data type {dtype}"),
    };

    public Type ReadPixelType()
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        Native.ErrorCode status;

        Native.GetHduType(_handle, out var hduType, out status);
        Native.ThrowIfNotOk(status);
        if (hduType != Native.HduType.IMAGE_HDU)
            throw new FitsException($"{hduType} are not supported");

        var naxes = new long[2];
        Native.ReadImageHeader(_handle, naxes.Length, out _, out var imageType, out _,
            naxes, out _, out _, out _, out status);
        Native.ThrowIfNotOk(status);

        var dataType = ResolveDataTypeFromImageType(imageType);
        return ConvertDataTypeToType(dataType);
    }

    public ImageHeader ReadHeader()
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        Native.ErrorCode status;

        var naxes = new long[2];
        Native.ReadImageHeader(_handle, naxes.Length, out var simple, out var imageType, out _,
            naxes, out _, out _, out var extended, out status);
        Native.ThrowIfNotOk(status);

        var header = new ImageHeader
        {
            FileExtension = ".fits",
            Width = (int) naxes[0],
            Height = (int) naxes[1],
        };

        Native.GetHeaderPosition(_handle, out var nkeys, out _, out status);
        Native.ThrowIfNotOk(status);

        const int EntryMaxBytes = 81;
        Span<byte> keywordSpan = stackalloc byte[EntryMaxBytes];
        Span<byte> valueSpan = stackalloc byte[EntryMaxBytes];
        Span<byte> commentSpan = stackalloc byte[EntryMaxBytes];

        for (int i = 1; i <= nkeys; i++)
        {
            status = Native.ErrorCode.OK;
            string keyword = string.Empty;
            string value = string.Empty;

            try
            {
                Native.ReadKeyByNumber(_handle, i, keywordSpan, valueSpan, commentSpan, out status);
                Native.ThrowIfNotOk(status);

                keyword = keywordSpan.NullTerminatedToString();
                value = valueSpan.NullTerminatedToString();
            }
            catch (FitsException fe)
            {
                Log.Warning(fe, "Error reading FITS header keyword {Count} in {Filename}", i, _filename);
                continue;
            }

            if (value.Length > 0)
            {
                try
                {
                    Native.GetKeyType(value, out var dtype, out status);
                    Native.ThrowIfNotOk(status);

                    var entryType = ResolveHeaderDatatype((char)dtype);
                    switch (entryType)
                    {
                        case var _ when entryType == typeof(string):
                            Native.ReadKeyString(_handle, keyword, valueSpan, commentSpan, out status);
                            Native.ThrowIfNotOk(status);
                            header.Items.Add(new StringHeaderEntry(
                                keyword: keyword,
                                comment: commentSpan.NullTerminatedToString(),
                                value: valueSpan.NullTerminatedToString()));
                            break;
                        case var _ when entryType == typeof(bool):
                            unsafe
                            {
                                int tempValue = 0;
                                Native.ReadKey(_handle, Native.DataType.TLOGICAL, keyword, &tempValue, commentSpan, out status);
                                Native.ThrowIfNotOk(status);
                                header.Items.Add(new BooleanHeaderEntry(
                                    keyword: keyword,
                                    comment: commentSpan.NullTerminatedToString(),
                                    value: tempValue == 1));
                            }
                            break;
                        case var _ when entryType == typeof(int):
                            unsafe
                            {
                                int tempValue = 0;
                                Native.ReadKey(_handle, Native.DataType.TINT, keyword, &tempValue, commentSpan, out status);
                                Native.ThrowIfNotOk(status);
                                header.Items.Add(new IntegerHeaderEntry(
                                    keyword: keyword,
                                    comment: commentSpan.NullTerminatedToString(),
                                    value: tempValue));
                            }
                            break;
                        case var _ when entryType == typeof(double):
                            unsafe
                            {
                                double tempValue = 0;
                                Native.ReadKey(_handle, Native.DataType.TDOUBLE, keyword, &tempValue, commentSpan, out status);
                                Native.ThrowIfNotOk(status);
                                header.Items.Add(new FloatHeaderEntry(
                                    keyword: keyword,
                                    comment: commentSpan.NullTerminatedToString(),
                                    value: tempValue));
                            }
                            break;
                    }
                }
                catch (FitsException fe)
                {
                    Log.Warning(fe, "Error reading FITS header keyword {Keyword} in {Filename}", keyword, _filename);
                    continue;
                }
            }
        }

        return header;
    }

    public void WriteHeader(ImageHeader header)
    {
        if (_handle == 0)
            throw new InvalidOperationException("No FITS file opened");

        if (_ioMode != IoMode.ReadWrite)
            throw new InvalidOperationException("FITS file is read only");

        foreach (var entry in header.Items)
        {
            if (ForbiddenKeywords.Any(entry.Keyword.Contains))
                continue;

            if (CfitsioDefaultComments.Any(entry.Comment.Equals))
                continue;

            Native.ErrorCode status;

            try
            {
                if (entry is StringHeaderEntry stringEntry)
                {
                    string stringValue = stringEntry.Value ?? string.Empty;
                    Native.WriteKeyString(_handle, entry.Keyword, stringValue, entry.Comment, out status);
                    Native.ThrowIfNotOk(status);
                }
                else if (entry is BooleanHeaderEntry boolEntry)
                {
                    unsafe
                    {
                        int value = boolEntry.Value ? 1 : 0;
                        Native.WriteKey(_handle, Native.DataType.TLOGICAL, entry.Keyword, &value, entry.Comment, out status);
                        Native.ThrowIfNotOk(status);
                    }
                }
                else if (entry is IntegerHeaderEntry integerEntry)
                {
                    unsafe
                    {
                        int value = integerEntry.Value;
                        Native.WriteKey(_handle, Native.DataType.TINT, entry.Keyword, &value, entry.Comment, out status);
                        Native.ThrowIfNotOk(status);
                    }
                }
                else if (entry is FloatHeaderEntry floatEntry)
                {
                    unsafe
                    {
                        double value = floatEntry.Value;
                        Native.WriteKey(_handle, Native.DataType.TDOUBLE, entry.Keyword, &value, entry.Comment, out status);
                        Native.ThrowIfNotOk(status);
                    }
                }
                else
                {
                    Log.Warning("Unsupported header entry type {EntryTypeName}", entry.GetType().Name);
                }
            }
            catch (FitsException fe)
            {
                Log.Debug(fe, "Error writing FITS header entry {Entry}", entry);
                continue;
            }
        }
    }

    public Memory2D<T> Read<T>()
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        Native.ErrorCode status;

        Native.GetHduType(_handle, out var hduType, out status);
        Native.ThrowIfNotOk(status);
        if (hduType != Native.HduType.IMAGE_HDU)
            throw new FitsException($"{hduType} is not supported");

        var naxes = new long[2];
        Native.ReadImageHeader(_handle, naxes.Length, out var simple, out var imageType, out _,
            naxes, out _, out _, out var extended, out status);
        Native.ThrowIfNotOk(status);

        var width = (int)naxes[0];
        var height = (int)naxes[1];
        var npixels = width * height;
        var dataType = ResolveDataTypeFromImageType(imageType);

        if (npixels == 0)
            throw new FitsException("NAXIS from FITS header is zero");

        var buffer = NativeMemoryAllocator<T>.Allocate(npixels);

        try
        {
            unsafe
            {
                void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer.Memory.Span));
                Native.ReadImage(_handle, dataType, 1, npixels, null, ptr, out _, out status);
                Native.ThrowIfNotOk(status);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error reading FITS image data frome {Filename}", _filename);
            buffer.Dispose();
            throw;
        }

        return new Memory2D<T>(buffer, width, height);
    }

    public Memory3D<T> Read3D<T>()
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        Native.ErrorCode status;

        Native.GetHduType(_handle, out var hduType, out status);
        Native.ThrowIfNotOk(status);
        if (hduType != Native.HduType.IMAGE_HDU)
            throw new FitsException($"{hduType} is not supported");

        var naxes = new long[3];
        Native.ReadImageHeader(_handle, naxes.Length, out var simple, out var imageType, out _,
            naxes, out _, out _, out var extended, out status);
        Native.ThrowIfNotOk(status);

        var width = (int)naxes[0];
        var height = (int)naxes[1];
        var channels = Math.Max(1, (int)naxes[2]);
        var npixelsPerChannel = width * height;
        var dataType = ResolveDataTypeFromImageType(imageType);

        if (channels == 1)
            throw new FitsException("Image is not multichannel");

        var buffers = new List<Memory2D<T>>(channels);

        try
        {
            for (int c = 0; c < channels; c++)
            {
                var buffer = NativeMemoryAllocator<T>.Allocate(npixelsPerChannel);

                unsafe
                {
                    void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer.Memory.Span));
                    Native.ReadImage(_handle, dataType, 1 + (c * npixelsPerChannel), npixelsPerChannel, null, ptr, out _, out status);
                    Native.ThrowIfNotOk(status);
                }

                buffers.Add(new Memory2D<T>(buffer, width, height));
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error reading multichannel FITS from {Filename}", _filename);
            foreach (var buffer in buffers)
                buffer.Dispose();
            buffers.Clear();
            throw;
        }

        return new Memory3D<T>(width, height, buffers);
    }

    public void Write<T>(Memory2D<T> memory)
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        if (_ioMode != IoMode.ReadWrite)
            throw new InvalidOperationException("FITS file is read only");

        var dataType = ResolveDataType(typeof(T));
        var imageType = ResolveImageType(typeof(T));
        var width = memory.Width;
        var height = memory.Height;
        var naxes = new long[] { width, height };
        var npixels = width * height;

        try
        {
            unsafe
            {
                Native.CreateImage(_handle, imageType, naxes.Length, naxes, out var status);
                Native.ThrowIfNotOk(status);

                void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(memory.GetSpan()));
                Native.WriteImage(_handle, dataType, 1, npixels, ptr, out status);
                Native.ThrowIfNotOk(status);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error writing FITS image data to {Filename}", _filename);
            throw;
        }
    }

    public void Write<T>(Memory3D<T> memory)
    {
        if (_handle == 0)
            throw new ObjectDisposedException("FITS image already disposed");

        if (_ioMode != IoMode.ReadWrite)
            throw new InvalidOperationException("FITS file is read only");

        var dataType = ResolveDataType(typeof(T));
        var imageType = ResolveImageType(typeof(T));
        var width = memory.Width;
        var height = memory.Height;
        var channels = memory.Channels;
        var naxes = new long[] { width, height, channels };
        var pixelsPerChannel = width * height;

        try
        {
            unsafe
            {
                Native.CreateImage(_handle, imageType, naxes.Length, naxes, out var status);
                Native.ThrowIfNotOk(status);

                for (int c = 0; c < channels; c++)
                {
                    void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(memory.GetSpan(c)));
                    Native.WriteImage(_handle, dataType, 1 + (c * pixelsPerChannel), pixelsPerChannel, ptr, out status);
                    Native.ThrowIfNotOk(status);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error writing multichannel FITS image data to {Filename}", _filename);
            throw;
        }
    }
}
