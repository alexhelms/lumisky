using CliWrap;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace OdinEye.Core.Video;

public class Ffmpeg
{
    private static string _ffmpegPath;

    private readonly ProcessPriorityClass _priority;

    private Channel<string> _stdoutChannel;
    private Channel<string> _stderrChannel;

    static Ffmpeg()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ffmpegPath = @"C:\ffmpeg\ffmpeg.exe";
        }
        else
        {
            _ffmpegPath = "/usr/bin/ffmpeg";
        }
    }

    public Ffmpeg(ProcessPriorityClass priority)
    {
        _priority = priority;
        _stdoutChannel = Channel.CreateUnbounded<string>();
        _stderrChannel = Channel.CreateUnbounded<string>();
    }

    public Ffmpeg()
        : this(ProcessPriorityClass.Normal)
    {
    }

    public string Output { get; private set; } = string.Empty;

    public TimeSpan Elapsed { get; private set; }

    public static void SetFfmpegPath(string path)
    {
        _ffmpegPath = path;
        CheckFfmpegPath();
    }

    private static void CheckFfmpegPath()
    {
        if (!File.Exists(_ffmpegPath))
            throw new FileNotFoundException("Ffmpeg not found", _ffmpegPath);
    }

    public async Task Run(string arguments, IProgress<FfmpegProgress>? progress = null, CancellationToken token = default)
    {
        CheckFfmpegPath();

        var stdoutBuilder = new StringBuilder(1024);
        var stderrBuilder = new StringBuilder(1024);
        
        _stdoutChannel = Channel.CreateUnbounded<string>();
        _stderrChannel = Channel.CreateUnbounded<string>();

        var start = Stopwatch.GetTimestamp();

        try
        {
            var stdout = PipeTarget.Create((stream, token) => WriteToChannel(stream, _stdoutChannel.Writer, token));
            var stderr = PipeTarget.Create((stream, token) => WriteToChannel(stream, _stderrChannel.Writer, token));

            var stdoutReader = ReadFromChannel(_stdoutChannel.Reader, OnStdout, token);
            var stderrReader = ReadFromChannel(_stderrChannel.Reader, OnStderr, token);

            // Ensure progress is available on stdout
            if (!arguments.Contains("-progress pipe:1"))
            {
                arguments = "-progress pipe:1 " + arguments;
            }

            var cmdTask = Cli.Wrap(_ffmpegPath)
                .WithArguments(arguments)
                .WithStandardOutputPipe(stdout)
                .WithStandardErrorPipe(stderr)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(token);

            try
            {
                if (Process.GetProcessById(cmdTask.ProcessId) is { } process)
                    process.PriorityClass = _priority;
            }
            catch
            {
                // TODO: log, could not set priority
            }

            // Wait for the process to complete
            var result = await cmdTask.ConfigureAwait(false);

            // Complete the channels
            _stdoutChannel.Writer.Complete();
            _stderrChannel.Writer.Complete();

            // Wait for the readers to exit
            await Task.WhenAll(stdoutReader).ConfigureAwait(false);
        }
        finally
        {
            // The normal ffmpeg output is on stderr, NOT stdout.
            // If the arguments have "-progress pipe:1" then special progress text is output on stdout.
            Output = stderrBuilder.ToString();

            Elapsed = Stopwatch.GetElapsedTime(start);
        }

        return;

        void OnStdout(string line)
        {
            if (progress is not null)
            {
                /*
					When "-progress pipe:1" is used, ffmpeg periodically prints the following to stdout:

					frame=46
					fps=0.00
					stream_0_0_q=32.0
					bitrate=   0.3kbits/s
					total_size=48
					out_time_us=1466667
					out_time_ms=1466667
					out_time=00:00:01.466667
					dup_frames=0
					drop_frames=0
					speed=2.79x
					progress=continue
				*/

                int? frame = null;
                if (line.StartsWith("frame"))
                {
                    int index = line.IndexOf('=');
                    if (index > -1)
                    {
                        frame = int.Parse(line.AsSpan().Slice(index + 1));
                    }
                }

                if (frame.HasValue)
                {
                    var ffmpegProgress = new FfmpegProgress
                    {
                        Frame = frame.Value,
                    };

                    progress.Report(ffmpegProgress);
                }
            }
        }

        void OnStderr(string line)
        {
            stderrBuilder.AppendLine(line);
        }
    }

    private async static Task WriteToChannel(Stream stream, ChannelWriter<string> writer, CancellationToken token)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            string? line;
            while ((line = await reader.ReadLineAsync(token).ConfigureAwait(false)) != null)
            {
                await writer.WriteAsync(line, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async static Task ReadFromChannel(ChannelReader<string> reader, Action<string> callback, CancellationToken token)
    {
        try
        {
            await foreach (var line in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                callback(line);
            }
        }
        catch (OperationCanceledException) { }
    }
}