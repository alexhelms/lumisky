using Serilog.Sinks.File;
using System.Text;

namespace LumiSky;

public class CaptureLogFilePathHook : FileLifecycleHooks
{
    public static string? Path { get; private set; }

    public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
    {
        Path = path;
        return base.OnFileOpened(path, underlyingStream, encoding);
    }
}
