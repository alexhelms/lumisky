using System.Runtime.InteropServices;

namespace LumiSky.Core.IO;

public sealed class TemporaryFile : IDisposable
{
    private bool _disposed;

    public TemporaryFile()
        : this(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()))
    {
    }

    public TemporaryFile(string path)
    {
        Path = path;
    }

    public static TemporaryFile TryCreateInTmfps()
    {
        // Try to use the tmpfs dir if it is available.
        // Tmpfs is a ramdisk and will be 1) faster and 2) reduce writes to sdcard/nand

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            Directory.Exists(LumiSkyPaths.Tmpfs))
        {
            var path = System.IO.Path.Combine(LumiSkyPaths.Tmpfs, Guid.NewGuid().ToString("N"));
            return new TemporaryFile(path);
        }
        else
        {
            return new TemporaryFile();
        }
    }

    public string Path { get; }

    ~TemporaryFile()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch
            {
                // best effort
            }

            _disposed = true;
        }
    }
}
