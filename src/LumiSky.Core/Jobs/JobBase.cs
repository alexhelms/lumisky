﻿using Quartz;

namespace LumiSky.Core.Jobs;

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
                OnSuccess(context);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception e)
            {
                Log.Error("Job Error: {Message}", e.Message);

                OnException(context, e);
                
                if (e is JobExecutionException)
                    throw;

                throw new JobExecutionException(e.Message, e, refireImmediately: RetryJobOnException);
            }
            finally
            {
                OnCompletion(context);
            }
        }
    }

    protected abstract Task OnExecute(IJobExecutionContext context);

    protected virtual void OnException(IJobExecutionContext context, Exception exception)
    {
        // empty
    }

    protected virtual void OnCompletion(IJobExecutionContext context)
    {
        // empty
    }

    protected virtual void OnSuccess(IJobExecutionContext context)
    {
        // empty
    }
}
