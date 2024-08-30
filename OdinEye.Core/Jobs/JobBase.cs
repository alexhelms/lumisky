using Quartz;

namespace OdinEye.Core.Jobs;

public abstract class JobBase : IJob
{
    protected int MaxRetries { get; set; } = 5;
    protected bool RetryJobOnException { get; set; } = true;

    public async Task  Execute(IJobExecutionContext context)
    {
        using (Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name))
        {
            if (context.CancellationToken.IsCancellationRequested) return;
            if (context.RefireCount > MaxRetries) return;

            try
            {
                await OnExecute(context);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception e)
            {
                throw new JobExecutionException(e.Message, e, refireImmediately: RetryJobOnException);
            }
        }
    }

    protected abstract Task OnExecute(IJobExecutionContext context);
}
