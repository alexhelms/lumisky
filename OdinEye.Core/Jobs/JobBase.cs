using Quartz;

namespace OdinEye.Core.Jobs;

public abstract class JobBase : IJob
{
    protected int MaxRetries { get; set; } = 5;
    protected bool RetryJobOnException { get; set; } = false;

    public async Task  Execute(IJobExecutionContext context)
    {
        using (Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name))
        {
            if (context.CancellationToken.IsCancellationRequested) return;
            if (context.RefireCount > MaxRetries) return;

            try
            {
                await OnExecute(context);
                OnCompletion(context);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception e)
            {
                Log.Error("Job Error: {Message}", e.Message);

                OnException(context);
                
                if (e is JobExecutionException)
                    throw;

                throw new JobExecutionException(e.Message, e, refireImmediately: RetryJobOnException);
            }
        }
    }

    protected abstract Task OnExecute(IJobExecutionContext context);

    protected virtual void OnException(IJobExecutionContext context)
    {
        // empty
    }

    protected virtual void OnCompletion(IJobExecutionContext context)
    {
        // empty
    }
}
