namespace JobScheduler.Core.Execution
{
    // job orcestrator, controls job lifecycle
    // TODO: claim job -> mark processing -> create JobExecutionContext -> find executor -> mark succeed/failed
    // FAILED: catch ex -> increment attempt -> if attempt < maxAttempts = mark retrying/scheduled, else mark failed
    internal class JobProcessor
    {
    }
}
