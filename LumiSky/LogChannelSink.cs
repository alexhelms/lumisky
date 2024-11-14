using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Templates;
using System.Text;
using System.Threading.Channels;

namespace LumiSky;

public class LogChannelSink : ILogEventSink, IDisposable
{
    private readonly LogChannel _channel;
    private readonly ITextFormatter _formatter;
    private readonly Channel<LogEvent> _serilogChannel;

    public LogChannelSink(LogChannel channel, ITextFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(formatter);

        _channel = channel;
        _formatter = formatter;
        _serilogChannel = Channel.CreateUnbounded<LogEvent>();

        _ = Task.Run(ProcessMessages);
    }

    public void Dispose()
    {
        _serilogChannel.Writer.Complete();
    }

    public void Emit(LogEvent logEvent)
    {
        while (!_serilogChannel.Writer.TryWrite(logEvent)) ;
    }

    private async Task ProcessMessages()
    {
        var sb = new StringBuilder(1024);

        await foreach (var logEvent in _serilogChannel.Reader.ReadAllAsync())
        {
            sb.Clear();
            using var writer = new StringWriter(sb);
            _formatter.Format(logEvent, writer);
            await _channel.Write(writer.ToString().TrimEnd());
        }
    }
}

public static class LogChannelSinkConfigurationExtensions
{
    public static LoggerConfiguration ChannelSink(
        this LoggerSinkConfiguration sinkConfiguration,
        LogChannel channel,
        string outputTemplate = "[{@t:HH:mm:ss.fff} {@l:u3}{#if SourceContext is not null} ({Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}){#end}] {@m}")
    {
        var formatter = new ExpressionTemplate(outputTemplate);
        var sink = new LogChannelSink(channel, formatter);
        return sinkConfiguration.Sink(sink);
    }
}

public class LogChannel
{
    private readonly Channel<string> _channel;

    public ChannelReader<string> Reader => _channel.Reader;

    public LogChannel()
    {
        _channel = Channel.CreateUnbounded<string>();
    }

    public async ValueTask Write(string content)
    {
        await _channel.Writer.WriteAsync(content);
    }
}
